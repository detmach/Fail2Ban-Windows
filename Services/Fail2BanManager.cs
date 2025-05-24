using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;
using Fail2Ban.Models;

namespace Fail2Ban.Services;

/// <summary>
/// Ana Fail2Ban yönetim servisi implementasyonu
/// </summary>
public class Fail2BanManager : IFail2BanManager
{
    private readonly ILogger<Fail2BanManager> _logger;
    private readonly Fail2BanSettings _settings;
    private readonly IFirewallManager _firewallManager;
    private readonly IAbuseReporter _abuseReporter;
    private readonly IServiceProvider _serviceProvider;
    
    // Thread-safe collections
    private readonly ConcurrentDictionary<string, EngellenenIP> _engellenenIpler = new();
    private readonly ConcurrentDictionary<string, HataliGiris> _hataliGirisler = new();
    
    // IP işleme senkronizasyonu için
    private readonly ConcurrentDictionary<string, object> _ipLocks = new();

    public Fail2BanManager(
        ILogger<Fail2BanManager> logger,
        IOptions<Fail2BanSettings> settings,
        IFirewallManager firewallManager,
        IAbuseReporter abuseReporter,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _firewallManager = firewallManager;
        _abuseReporter = abuseReporter;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> RecordFailedAttemptAsync(string ipAdresi, string filterAdi)
    {
        if (string.IsNullOrWhiteSpace(ipAdresi))
            return false;

        // IP bazında senkronizasyon sağla
        var ipLock = _ipLocks.GetOrAdd(ipAdresi, _ => new object());
        
        lock (ipLock)
        {
            // Memory'de kontrol et - hızlı kontrol
            if (_engellenenIpler.ContainsKey(ipAdresi))
            {
                _logger.LogDebug("IP adresi memory'de zaten engellenmiş: {IpAdresi}", ipAdresi);
                return false;
            }
        }

        // Veritabanından kontrol et - IP zaten banlanmış mı? (yeni scope ile)
        using var scope = _serviceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        
        var veritabanindaBanli = await databaseService.IpBanliMiAsync(ipAdresi);
        if (veritabanindaBanli)
        {
            _logger.LogDebug("IP adresi veritabanında zaten banlanmış: {IpAdresi}", ipAdresi);
            return false;
        }

        // IP bazında tekrar senkronize kontrol
        lock (ipLock)
        {
            // Double-check locking pattern - memory'de tekrar kontrol
            if (_engellenenIpler.ContainsKey(ipAdresi))
            {
                _logger.LogDebug("IP adresi memory'de zaten engellenmiş (double-check): {IpAdresi}", ipAdresi);
                return false;
            }

            // Hatalı giriş kaydını güncelle
            var hataliGiris = _hataliGirisler.AddOrUpdate(ipAdresi,
                new HataliGiris 
                { 
                    IpAdresi = ipAdresi, 
                    FilterAdi = filterAdi,
                    HataSayisi = 1,
                    IlkHataTarihi = DateTime.Now,
                    SonHataTarihi = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.HataArtir();
                    existing.FilterAdi = filterAdi; // Son kullanılan filtreyi kaydet
                    return existing;
                });

            _logger.LogDebug("Hatalı giriş kaydedildi - IP: {IpAdresi}, Sayı: {HataSayisi}, Filtre: {FilterAdi}", 
                ipAdresi, hataliGiris.HataSayisi, filterAdi);

            // Maksimum hata sayısını aşmış mı kontrol et
            var maxHata = GetMaxHataForFilter(filterAdi);
            if (hataliGiris.HataSayisi >= maxHata)
            {
                // Hemen memory'e blocking marker ekle (diğer thread'leri engelle)
                var tempBlockedIp = new EngellenenIP
                {
                    IpAdresi = ipAdresi,
                    EngellenmeTarihi = DateTime.Now,
                    EngellemeBitisTarihi = DateTime.Now.AddYears(1), // Geçici - async işlem bitince düzeltilir
                    FilterAdi = "PROCESSING",
                    ToplamHataSayisi = hataliGiris.HataSayisi
                };
                
                _engellenenIpler.TryAdd(ipAdresi, tempBlockedIp);
                
                // Async ban işlemini başlat
                var engellemeSuresi = GetEngellemeZamaniForFilter(filterAdi);
                _ = Task.Run(async () => await BlockIpInternalAsync(ipAdresi, engellemeSuresi, filterAdi, hataliGiris.HataSayisi));
                
                return true;
            }

            return false;
        }
    }

    public async Task<bool> BlockIpManuallyAsync(string ipAdresi, int engellemeSuresi, string sebep = "Manuel Engelleme")
    {
        return await BlockIpInternalAsync(ipAdresi, engellemeSuresi, sebep, 0);
    }

    public async Task<bool> UnblockIpManuallyAsync(string ipAdresi)
    {
        // Önce veritabanından ban kaydını pasif yap (yeni scope ile)
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            await databaseService.BanKaydiPasifYapAsync(ipAdresi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanında ban kaydı pasif yapılırken hata: {IpAdresi}", ipAdresi);
        }

        if (!_engellenenIpler.TryRemove(ipAdresi, out var engellenenIp))
        {
            _logger.LogWarning("Kaldırılacak engellenmiş IP memory'de bulunamadı: {IpAdresi}", ipAdresi);
            // Yine de firewall'dan kaldırmayı dene
        }

        var success = await _firewallManager.UnblockIpAsync(ipAdresi);
        if (success)
        {
            _logger.LogInformation("IP adresinin engellemesi manuel olarak kaldırıldı: {IpAdresi}", ipAdresi);
            
            // Hatalı giriş kaydını da temizle
            _hataliGirisler.TryRemove(ipAdresi, out _);
        }
        else
        {
            // Başarısız olursa memory'e geri ekle
            if (engellenenIp != null)
            {
                _engellenenIpler.TryAdd(ipAdresi, engellenenIp);
            }
        }

        return success;
    }

    public async Task<int> CleanupExpiredBlocksAsync()
    {
        // Önce veritabanındaki süresi dolmuş banları pasif yap (yeni scope ile)
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            await databaseService.SuresiDolmusBanlariPasifYapAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanında süresi dolmuş banlar pasif yapılırken hata");
        }

        var expiredIps = _engellenenIpler.Values
            .Where(ip => !ip.AktifMi)
            .Select(ip => ip.IpAdresi)
            .ToList();

        var cleanedCount = 0;

        foreach (var ipAdresi in expiredIps)
        {
            if (_engellenenIpler.TryRemove(ipAdresi, out _))
            {
                var success = await _firewallManager.UnblockIpAsync(ipAdresi);
                if (success)
                {
                    cleanedCount++;
                    _logger.LogInformation("Süresi dolan IP engellemesi temizlendi: {IpAdresi}", ipAdresi);
                    
                    // Hatalı giriş kaydını da temizle
                    _hataliGirisler.TryRemove(ipAdresi, out _);
                }
                else
                {
                    _logger.LogWarning("Süresi dolan IP engellemesi temizlenemedi: {IpAdresi}", ipAdresi);
                }
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation("Toplam {Count} süresi dolan IP engellemesi temizlendi", cleanedCount);
        }

        return cleanedCount;
    }

    public List<EngellenenIP> GetBlockedIps()
    {
        return _engellenenIpler.Values.ToList();
    }

    public List<HataliGiris> GetFailedAttempts()
    {
        return _hataliGirisler.Values.ToList();
    }

    public EngellenenIP? GetBlockedIpInfo(string ipAdresi)
    {
        _engellenenIpler.TryGetValue(ipAdresi, out var engellenenIp);
        return engellenenIp;
    }

    /// <summary>
    /// Başlangıçta veritabanından aktif banları yükler
    /// </summary>
    public async Task InitializeFromDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Veritabanından aktif ban kayıtları yükleniyor...");
            
            // Yeni scope ile database işlemi
            using var scope = _serviceProvider.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            
            var aktifBanlar = await databaseService.GetAktifBanKayitlariAsync();
            
            foreach (var ban in aktifBanlar)
            {
                // Memory'e yükle
                var engellenenIp = new EngellenenIP
                {
                    IpAdresi = ban.IpAdresi,
                    EngellenmeTarihi = ban.YasaklamaZamani,
                    EngellemeBitisTarihi = ban.SilmeZamani ?? DateTime.MaxValue,
                    FilterAdi = ban.KuralAdi,
                    ToplamHataSayisi = ban.BasarisizGirisSayisi
                };

                _engellenenIpler.TryAdd(ban.IpAdresi, engellenenIp);
                
                // Firewall'a da ekle (eğer yoksa)
                await _firewallManager.BlockIpAsync(ban.IpAdresi);
            }
            
            _logger.LogInformation("Veritabanından {Count} aktif ban kaydı yüklendi", aktifBanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanından ban kayıtları yüklenirken hata");
        }
    }

    /// <summary>
    /// IP adresini dahili olarak engeller
    /// </summary>
    private async Task<bool> BlockIpInternalAsync(string ipAdresi, int engellemeSuresi, string sebep, int hataSayisi)
    {
        try
        {
            _logger.LogDebug("IP adresi engelleme başlatılıyor: {IpAdresi}, Sebep: {Sebep}", ipAdresi, sebep);

            // Önce firewall'da engelle
            var success = await _firewallManager.BlockIpAsync(ipAdresi);
            if (!success)
            {
                _logger.LogError("IP adresi firewall'da engellenemedi: {IpAdresi}", ipAdresi);
                
                // Başarısız olursa memory'den geçici kaydı kaldır
                _engellenenIpler.TryRemove(ipAdresi, out _);
                return false;
            }

            // Doğru engellenen IP bilgilerini oluştur
            var engellenenIp = new EngellenenIP
            {
                IpAdresi = ipAdresi,
                EngellenmeTarihi = DateTime.Now,
                EngellemeBitisTarihi = DateTime.Now.AddSeconds(engellemeSuresi),
                FilterAdi = sebep,
                ToplamHataSayisi = hataSayisi
            };

            // Memory'deki geçici kaydı doğru bilgilerle güncelle
            _engellenenIpler.TryUpdate(ipAdresi, engellenenIp, _engellenenIpler[ipAdresi]);

            // Veritabanına ekle (yeni scope ile)
            BanKaydi? banKaydi = null;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                
                // Önce veritabanında zaten ban kaydı var mı kontrol et
                var mevcutBan = await databaseService.IpBanliMiAsync(ipAdresi);
                if (mevcutBan)
                {
                    _logger.LogInformation("IP adresi veritabanında zaten banlanmış, yeni kayıt eklenmedi: {IpAdresi}", ipAdresi);
                }
                else
                {
                    banKaydi = await databaseService.BanKaydiEkleAsync(
                        ipAdresi, 
                        sebep, 
                        engellemeSuresi / 60, // saniyeyi dakikaya çevir
                        hataSayisi,
                        $"Firewall engelleme aktif - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ban kaydı veritabanına eklenirken hata: {IpAdresi}", ipAdresi);
                // Veritabanı hatası olsa da firewall engellemesi devam eder
            }
            
            _logger.LogWarning("IP adresi engellendi - IP: {IpAdresi}, Süre: {Dakika} dakika, Sebep: {Sebep}", 
                ipAdresi, engellenenIp.EngellemeDAkika, sebep);

            // Hatalı giriş kaydını temizle
            _hataliGirisler.TryRemove(ipAdresi, out _);

            // AbuseIPDB'ye rapor gönder (duplicate kontrollü, yeni scope ile)
            if (_abuseReporter.IsEnabled() && banKaydi != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Her background task için yeni scope
                        using var backgroundScope = _serviceProvider.CreateScope();
                        var backgroundDatabaseService = backgroundScope.ServiceProvider.GetRequiredService<IDatabaseService>();
                        
                        // Son 24 saat içinde aynı IP için rapor gönderilmiş mi kontrol et
                        var sonRaporlandi = await backgroundDatabaseService.IpSonRaporlandiMiAsync(ipAdresi, 24);
                        if (sonRaporlandi)
                        {
                            _logger.LogInformation("IP adresi son 24 saat içinde AbuseIPDB'ye raporlandığı için tekrar raporlanmıyor: {IpAdresi}", ipAdresi);
                            return;
                        }

                        // AbuseIPDB'ye rapor gönder
                        var raporBasarili = await _abuseReporter.ReportBanAsync(banKaydi);
                        
                        // Rapor başarılıysa veritabanında işaretle
                        if (raporBasarili)
                        {
                            await backgroundDatabaseService.BanKaydiAbuseRaporlandiAsync(banKaydi.Id);
                            _logger.LogInformation("IP adresi AbuseIPDB'ye başarıyla raporlandı: {IpAdresi}", ipAdresi);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AbuseIPDB raporu gönderilirken hata oluştu - IP: {IpAdresi}, Kural: {KuralAdi}", ipAdresi, sebep);
                    }
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi engellenirken beklenmedik hata oluştu: {IpAdresi}", ipAdresi);
            
            // Hata durumunda memory'den geçici kaydı kaldır
            _engellenenIpler.TryRemove(ipAdresi, out _);
            return false;
        }
    }

    /// <summary>
    /// Filtre için maksimum hata sayısını döndürür
    /// </summary>
    private int GetMaxHataForFilter(string filterAdi)
    {
        var filter = _settings.LogFiltreler.FirstOrDefault(f => f.Ad == filterAdi);
        return filter?.OzelMaxHata ?? _settings.MaxHataliGiris;
    }

    /// <summary>
    /// Filtre için engelleme süresini döndürür
    /// </summary>
    private int GetEngellemeZamaniForFilter(string filterAdi)
    {
        var filter = _settings.LogFiltreler.FirstOrDefault(f => f.Ad == filterAdi);
        return filter?.OzelEngellemeSuresi ?? _settings.EngellemeZamani;
    }
} 