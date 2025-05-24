using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Fail2Ban.Data;
using Fail2Ban.Interfaces;
using Fail2Ban.Models;

namespace Fail2Ban.Services;

/// <summary>
/// Veritabanı işlemleri servisi
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly Fail2BanDbContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(Fail2BanDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IpBanliMiAsync(string ipAdresi)
    {
        try
        {
            var aktifBan = await _context.BanKayitlari
                .AnyAsync(b => b.IpAdresi == ipAdresi && 
                             b.Aktif && 
                             (b.SilmeZamani == null || b.SilmeZamani > DateTime.Now));
                             
            return aktifBan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP ban kontrolü sırasında hata: {IpAdresi}", ipAdresi);
            return false;
        }
    }

    public async Task<BanKaydi?> GetAktifBanKaydiAsync(string ipAdresi)
    {
        try
        {
            return await _context.BanKayitlari
                .FirstOrDefaultAsync(b => b.IpAdresi == ipAdresi && 
                                        b.Aktif && 
                                        (b.SilmeZamani == null || b.SilmeZamani > DateTime.Now));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif ban kaydı sorgulanırken hata: {IpAdresi}", ipAdresi);
            return null;
        }
    }

    public async Task<BanKaydi> BanKaydiEkleAsync(string ipAdresi, string kuralAdi, int banSuresiDakika, int basarisizGirisSayisi, string? notlar = null)
    {
        try
        {
            // Önce mevcut aktif ban var mı kontrol et
            var mevcutBan = await GetAktifBanKaydiAsync(ipAdresi);
            if (mevcutBan != null)
            {
                _logger.LogWarning("IP zaten banlanmış: {IpAdresi}", ipAdresi);
                return mevcutBan;
            }

            var banKaydi = new BanKaydi
            {
                IpAdresi = ipAdresi,
                KuralAdi = kuralAdi,
                BanSuresiDakika = banSuresiDakika,
                BasarisizGirisSayisi = basarisizGirisSayisi,
                Notlar = notlar,
                YasaklamaZamani = DateTime.Now,
                SilmeZamani = banSuresiDakika > 0 ? DateTime.Now.AddMinutes(banSuresiDakika) : null,
                Aktif = true,
                OlusturmaZamani = DateTime.Now
            };

            _context.BanKayitlari.Add(banKaydi);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Yeni ban kaydı eklendi: {IpAdresi}, Kural: {KuralAdi}, Süre: {BanSuresiDakika} dakika", 
                ipAdresi, kuralAdi, banSuresiDakika);

            return banKaydi;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban kaydı eklenirken hata: {IpAdresi}", ipAdresi);
            throw;
        }
    }

    public async Task BanKaydiGuncelleAsync(BanKaydi banKaydi)
    {
        try
        {
            banKaydi.GuncellemeZamani = DateTime.Now;
            _context.BanKayitlari.Update(banKaydi);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ban kaydı güncellendi: {IpAdresi}", banKaydi.IpAdresi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban kaydı güncellenirken hata: {IpAdresi}", banKaydi.IpAdresi);
            throw;
        }
    }

    public async Task BanKaydiPasifYapAsync(string ipAdresi)
    {
        try
        {
            var banKaydi = await GetAktifBanKaydiAsync(ipAdresi);
            if (banKaydi != null)
            {
                banKaydi.Aktif = false;
                banKaydi.GuncellemeZamani = DateTime.Now;
                banKaydi.SilmeZamani = DateTime.Now;

                await BanKaydiGuncelleAsync(banKaydi);
                
                _logger.LogInformation("Ban kaydı pasif yapıldı: {IpAdresi}", ipAdresi);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban kaydı pasif yapılırken hata: {IpAdresi}", ipAdresi);
            throw;
        }
    }

    public async Task SuresiDolmusBanlariPasifYapAsync()
    {
        try
        {
            var suresiDolmuslar = await _context.BanKayitlari
                .Where(b => b.Aktif && 
                           b.SilmeZamani != null && 
                           b.SilmeZamani <= DateTime.Now)
                .ToListAsync();

            foreach (var ban in suresiDolmuslar)
            {
                ban.Aktif = false;
                ban.GuncellemeZamani = DateTime.Now;
            }

            if (suresiDolmuslar.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Süresi dolmuş {Count} ban kaydı pasif yapıldı", suresiDolmuslar.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Süresi dolmuş ban kayıtları pasif yapılırken hata");
            throw;
        }
    }

    public async Task<List<BanKaydi>> GetAktifBanKayitlariAsync()
    {
        try
        {
            return await _context.BanKayitlari
                .Where(b => b.Aktif && (b.SilmeZamani == null || b.SilmeZamani > DateTime.Now))
                .OrderByDescending(b => b.YasaklamaZamani)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktif ban kayıtları sorgulanırken hata");
            return new List<BanKaydi>();
        }
    }

    public async Task<List<BanKaydi>> GetBanKayitlariAsync(DateTime baslangic, DateTime bitis)
    {
        try
        {
            return await _context.BanKayitlari
                .Where(b => b.YasaklamaZamani >= baslangic && b.YasaklamaZamani <= bitis)
                .OrderByDescending(b => b.YasaklamaZamani)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban kayıtları sorgulanırken hata: {Baslangic} - {Bitis}", baslangic, bitis);
            return new List<BanKaydi>();
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Veritabanı başarıyla başlatıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanı başlatılırken hata");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetIstatistiklerAsync()
    {
        try
        {
            var istatistikler = new Dictionary<string, object>();

            var toplamBan = await _context.BanKayitlari.CountAsync();
            var aktifBan = await _context.BanKayitlari.CountAsync(b => b.Aktif);
            var bugunBan = await _context.BanKayitlari.CountAsync(b => b.YasaklamaZamani.Date == DateTime.Today);
            var buHaftaBan = await _context.BanKayitlari.CountAsync(b => b.YasaklamaZamani >= DateTime.Today.AddDays(-7));

            // En çok ban yiyen IP'ler
            var topIpBanlar = await _context.BanKayitlari
                .GroupBy(b => b.IpAdresi)
                .Select(g => new { IpAdresi = g.Key, BanSayisi = g.Count() })
                .OrderByDescending(x => x.BanSayisi)
                .Take(10)
                .ToListAsync();

            // En çok tetiklenen kurallar
            var topKurallar = await _context.BanKayitlari
                .GroupBy(b => b.KuralAdi)
                .Select(g => new { KuralAdi = g.Key, BanSayisi = g.Count() })
                .OrderByDescending(x => x.BanSayisi)
                .Take(10)
                .ToListAsync();

            istatistikler["ToplamBan"] = toplamBan;
            istatistikler["AktifBan"] = aktifBan;
            istatistikler["BugunBan"] = bugunBan;
            istatistikler["BuHaftaBan"] = buHaftaBan;
            istatistikler["TopIpBanlar"] = topIpBanlar;
            istatistikler["TopKurallar"] = topKurallar;

            return istatistikler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstatistikler hesaplanırken hata");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// IP adresinin son 24 saat içinde AbuseIPDB'ye raporlanıp raporlanmadığını kontrol eder
    /// </summary>
    public async Task<bool> IpSonRaporlandiMiAsync(string ipAdresi, int saatSiniri = 24)
    {
        try
        {
            var sinirTarihi = DateTime.Now.AddHours(-saatSiniri);
            
            var sonRapor = await _context.BanKayitlari
                .Where(b => b.IpAdresi == ipAdresi && 
                           b.AbuseIPDBRaporTarihi != null && 
                           b.AbuseIPDBRaporTarihi > sinirTarihi)
                .AnyAsync();
                
            return sonRapor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP son rapor kontrolü sırasında hata: {IpAdresi}", ipAdresi);
            return true; // Hata durumunda rapor gönderilmemesi için true döner
        }
    }

    /// <summary>
    /// Ban kaydını AbuseIPDB'ye raporlandı olarak işaretler
    /// </summary>
    public async Task BanKaydiAbuseRaporlandiAsync(int banKaydiId)
    {
        try
        {
            var banKaydi = await _context.BanKayitlari.FindAsync(banKaydiId);
            if (banKaydi != null)
            {
                banKaydi.AbuseIPDBRaporTarihi = DateTime.Now;
                banKaydi.GuncellemeZamani = DateTime.Now;
                
                await _context.SaveChangesAsync();
                _logger.LogDebug("Ban kaydı AbuseIPDB'ye raporlandı olarak işaretlendi: {IpAdresi}", banKaydi.IpAdresi);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ban kaydı AbuseIPDB rapor işaretlemesi sırasında hata: {BanKaydiId}", banKaydiId);
        }
    }

    /// <summary>
    /// Belirli IP için en son ban kaydını getirir
    /// </summary>
    public async Task<BanKaydi?> GetSonBanKaydiAsync(string ipAdresi)
    {
        try
        {
            return await _context.BanKayitlari
                .Where(b => b.IpAdresi == ipAdresi)
                .OrderByDescending(b => b.YasaklamaZamani)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son ban kaydı sorgulanırken hata: {IpAdresi}", ipAdresi);
            return null;
        }
    }
} 