using System.ComponentModel.DataAnnotations;

namespace Fail2Ban.Models;

/// <summary>
/// IP ban kayıtlarını veritabanında saklayan model
/// </summary>
public class BanKaydi
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Engellenen IP adresi
    /// </summary>
    [Required]
    [StringLength(45)] // IPv6 için maksimum uzunluk
    public required string IpAdresi { get; set; }
    
    /// <summary>
    /// Ban başlangıç zamanı
    /// </summary>
    public DateTime YasaklamaZamani { get; set; }
    
    /// <summary>
    /// Ban kalkış zamanı (null ise süresiz)
    /// </summary>
    public DateTime? SilmeZamani { get; set; }
    
    /// <summary>
    /// Kaç dakika süre ile banlandı
    /// </summary>
    public int BanSuresiDakika { get; set; }
    
    /// <summary>
    /// Hangi konfigürasyon kuralı ile banlandı
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string KuralAdi { get; set; }
    
    /// <summary>
    /// Ban aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Kaydın oluşturulma zamanı
    /// </summary>
    public DateTime OlusturmaZamani { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime? GuncellemeZamani { get; set; }
    
    /// <summary>
    /// Ek notlar
    /// </summary>
    [StringLength(500)]
    public string? Notlar { get; set; }
    
    /// <summary>
    /// Kaç kez başarısız giriş yapıldı
    /// </summary>
    public int BasarisizGirisSayisi { get; set; }
    
    /// <summary>
    /// AbuseIPDB'ye ne zaman raporlandı (null ise henüz raporlanmadı)
    /// </summary>
    public DateTime? AbuseIPDBRaporTarihi { get; set; }
} 