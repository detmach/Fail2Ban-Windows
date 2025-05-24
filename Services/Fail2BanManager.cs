using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    
    // Thread-safe collections
    private readonly ConcurrentDictionary<string, EngellenenIP> _engellenenIpler = new();
    private readonly ConcurrentDictionary<string, HataliGiris> _hataliGirisler = new();

    public Fail2BanManager(
        ILogger<Fail2BanManager> logger,
        IOptions<Fail2BanSettings> settings,
        IFirewallManager firewallManager,
        IAbuseReporter abuseReporter)
    {
        _logger = logger;
        _settings = settings.Value;
        _firewallManager = firewallManager;
        _abuseReporter = abuseReporter;
    }

    public async Task<bool> RecordFailedAttemptAsync(string ipAdresi, string filterAdi)
    {
        if (string.IsNullOrWhiteSpace(ipAdresi))
            return false;

        // Zaten engellenmiş mi kontrol et
        if (_engellenenIpler.ContainsKey(ipAdresi))
        {
            _logger.LogDebug("IP adresi zaten engellenmiş: {IpAdresi}", ipAdresi);
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

        // Maksimum hata sayısını aş mış mı kontrol et
        var maxHata = GetMaxHataForFilter(filterAdi);
        if (hataliGiris.HataSayisi >= maxHata)
        {
            var engellemeSuresi = GetEngellemeZamaniForFilter(filterAdi);
            return await BlockIpInternalAsync(ipAdresi, engellemeSuresi, filterAdi, hataliGiris.HataSayisi);
        }

        return false;
    }

    public async Task<bool> BlockIpManuallyAsync(string ipAdresi, int engellemeSuresi, string sebep = "Manuel Engelleme")
    {
        return await BlockIpInternalAsync(ipAdresi, engellemeSuresi, sebep, 0);
    }

    public async Task<bool> UnblockIpManuallyAsync(string ipAdresi)
    {
        if (!_engellenenIpler.TryRemove(ipAdresi, out var engellenenIp))
        {
            _logger.LogWarning("Kaldırılacak engellenmiş IP bulunamadı: {IpAdresi}", ipAdresi);
            return false;
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
            // Başarısız olursa geri ekle
            _engellenenIpler.TryAdd(ipAdresi, engellenenIp);
        }

        return success;
    }

    public async Task<int> CleanupExpiredBlocksAsync()
    {
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
    /// IP adresini dahili olarak engeller
    /// </summary>
    private async Task<bool> BlockIpInternalAsync(string ipAdresi, int engellemeSuresi, string sebep, int hataSayisi)
    {
        try
        {
            var success = await _firewallManager.BlockIpAsync(ipAdresi);
            if (!success)
            {
                _logger.LogError("IP adresi firewall'da engellenemedi: {IpAdresi}", ipAdresi);
                return false;
            }

            var engellenenIp = new EngellenenIP
            {
                IpAdresi = ipAdresi,
                EngellenmeTarihi = DateTime.Now,
                EngellemeBitisTarihi = DateTime.Now.AddSeconds(engellemeSuresi),
                FilterAdi = sebep,
                ToplamHataSayisi = hataSayisi
            };

            _engellenenIpler.TryAdd(ipAdresi, engellenenIp);
            
            _logger.LogWarning("IP adresi engellendi - IP: {IpAdresi}, Süre: {Dakika} dakika, Sebep: {Sebep}", 
                ipAdresi, engellenenIp.EngellemeDAkika, sebep);

            // Hatalı giriş kaydını temizle
            _hataliGirisler.TryRemove(ipAdresi, out _);

            // AbuseIPDB'ye rapor gönder
            if (_abuseReporter.IsEnabled())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _abuseReporter.ReportIpAsync(engellenenIp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AbuseIPDB raporu gönderilirken hata oluştu: {IpAdresi}", ipAdresi);
                    }
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi engellenirken beklenmedik hata oluştu: {IpAdresi}", ipAdresi);
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