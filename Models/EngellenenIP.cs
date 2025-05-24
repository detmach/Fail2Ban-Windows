namespace Fail2Ban.Models;

/// <summary>
/// Engellenmiş IP adresi bilgilerini temsil eder
/// </summary>
public class EngellenenIP
{
    /// <summary>
    /// IP adresi
    /// </summary>
    public string IpAdresi { get; set; } = string.Empty;
    
    /// <summary>
    /// Engellenme zamanı
    /// </summary>
    public DateTime EngellenmeTarihi { get; set; }
    
    /// <summary>
    /// Engellemenin sona ereceği tarih
    /// </summary>
    public DateTime EngellemeBitisTarihi { get; set; }
    
    /// <summary>
    /// Hangi filtre tarafından engellendiği
    /// </summary>
    public string FilterAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Toplam hata sayısı
    /// </summary>
    public int ToplamHataSayisi { get; set; }
    
    /// <summary>
    /// Firewall kuralı adı
    /// </summary>
    public string FirewallKuralAdi => $"Fail2Ban_{IpAdresi}";
    
    /// <summary>
    /// Engelleme süresi dakika cinsinden
    /// </summary>
    public int EngellemeDAkika => (int)(EngellemeBitisTarihi - EngellenmeTarihi).TotalMinutes;
    
    /// <summary>
    /// Engelleme aktif mi?
    /// </summary>
    public bool AktifMi => DateTime.Now < EngellemeBitisTarihi;
} 