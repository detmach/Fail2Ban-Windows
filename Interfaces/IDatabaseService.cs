using Fail2Ban.Models;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Veritabanı işlemleri interface'i
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// IP adresi daha önce banlanmış mı kontrol et
    /// </summary>
    Task<bool> IpBanliMiAsync(string ipAdresi);
    
    /// <summary>
    /// Aktif ban kaydı var mı kontrol et
    /// </summary>
    Task<BanKaydi?> GetAktifBanKaydiAsync(string ipAdresi);
    
    /// <summary>
    /// Yeni ban kaydı ekle
    /// </summary>
    Task<BanKaydi> BanKaydiEkleAsync(string ipAdresi, string kuralAdi, int banSuresiDakika, int basarisizGirisSayisi, string? notlar = null);
    
    /// <summary>
    /// Ban kaydını güncelle
    /// </summary>
    Task BanKaydiGuncelleAsync(BanKaydi banKaydi);
    
    /// <summary>
    /// Ban kaydını pasif yap (silme zamanı set et)
    /// </summary>
    Task BanKaydiPasifYapAsync(string ipAdresi);
    
    /// <summary>
    /// Süresi dolmuş ban kayıtlarını pasif yap
    /// </summary>
    Task SuresiDolmusBanlariPasifYapAsync();
    
    /// <summary>
    /// Aktif ban kayıtlarını getir
    /// </summary>
    Task<List<BanKaydi>> GetAktifBanKayitlariAsync();
    
    /// <summary>
    /// Belirli tarih aralığındaki ban kayıtlarını getir
    /// </summary>
    Task<List<BanKaydi>> GetBanKayitlariAsync(DateTime baslangic, DateTime bitis);
    
    /// <summary>
    /// Veritabanını initialize et
    /// </summary>
    Task InitializeDatabaseAsync();
    
    /// <summary>
    /// İstatistikleri getir
    /// </summary>
    Task<Dictionary<string, object>> GetIstatistiklerAsync();
    
    /// <summary>
    /// IP adresinin son belirtilen saat içinde AbuseIPDB'ye raporlanıp raporlanmadığını kontrol eder
    /// </summary>
    Task<bool> IpSonRaporlandiMiAsync(string ipAdresi, int saatSiniri = 24);
    
    /// <summary>
    /// Ban kaydını AbuseIPDB'ye raporlandı olarak işaretler
    /// </summary>
    Task BanKaydiAbuseRaporlandiAsync(int banKaydiId);
    
    /// <summary>
    /// Belirli IP için en son ban kaydını getirir
    /// </summary>
    Task<BanKaydi?> GetSonBanKaydiAsync(string ipAdresi);
} 