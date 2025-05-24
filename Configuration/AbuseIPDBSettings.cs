namespace Fail2Ban.Configuration;

/// <summary>
/// AbuseIPDB servis ayarları
/// </summary>
public class AbuseIPDBSettings
{
    /// <summary>
    /// AbuseIPDB API anahtarı
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// AbuseIPDB API URL'si
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.abuseipdb.com/api/v2/report";
    
    /// <summary>
    /// Raporlama kategorisi (18 = Brute Force)
    /// </summary>
    public int Kategori { get; set; } = 18;
    
    /// <summary>
    /// AbuseIPDB raporlaması aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = false;
    
    /// <summary>
    /// Rapor açıklaması şablonu
    /// </summary>
    public string RaporSablonu { get; set; } = "SMTP Brute Force Attack was blocked. IP has been banned for {0} minutes at {1}";
} 