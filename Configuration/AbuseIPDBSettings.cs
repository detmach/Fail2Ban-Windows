namespace Fail2Ban.Configuration;

/// <summary>
/// AbuseIPDB API ayarları
/// </summary>
public class AbuseIPDBSettings
{
    /// <summary>
    /// AbuseIPDB API anahtarı
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// AbuseIPDB API URL'i
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.abuseipdb.com/api/v2/report";
    
    /// <summary>
    /// Saldırı kategorisi (18 = Brute Force)
    /// </summary>
    public int Kategori { get; set; } = 18;
    
    /// <summary>
    /// AbuseIPDB raporlama aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Aynı IP için minimum rapor aralığı (saat cinsinden)
    /// </summary>
    public int MinRaporAraligiSaat { get; set; } = 24;
    
    /// <summary>
    /// Varsayılan rapor mesaj şablonu (eski uyumluluk için)
    /// </summary>
    [Obsolete("Bu özellik artık kullanılmıyor. SistemMesajlari kullanın.")]
    public string RaporSablonu { get; set; } = "Attack was blocked. IP has been banned for {0} minutes at {1}";
    
    /// <summary>
    /// Ban sistemlerine göre özel mesaj şablonları
    /// </summary>
    public Dictionary<string, string> SistemMesajlari { get; set; } = new()
    {
        // SMTP saldırıları
        ["SMTP-AUTH-Failed"] = "SMTP Authentication attack detected. Multiple failed login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        ["SMTP-Brute-Force"] = "SMTP Brute Force attack detected. Aggressive login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        
        // Event Log saldırıları  
        ["EventLog-RDP"] = "RDP Brute Force attack detected. Multiple failed Remote Desktop login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        ["EventLog-Network"] = "Network login attack detected. Multiple failed network authentication attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        ["EventLog-Kerberos"] = "Kerberos authentication attack detected. Multiple failed Kerberos pre-authentication attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        ["EventLog-SQLServer"] = "SQL Server Brute Force attack detected. Multiple failed database login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        ["EventLog-Other"] = "Windows authentication attack detected. Multiple failed login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
        
        // Varsayılan mesaj
        ["Default"] = "Security attack detected. Multiple failed attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}"
    };
    
    /// <summary>
    /// Ban sistemine göre uygun mesaj şablonunu getir
    /// </summary>
    /// <param name="kuralAdi">Ban kuralının adı</param>
    /// <returns>Mesaj şablonu</returns>
    public string GetMesajSablonu(string kuralAdi)
    {
        if (SistemMesajlari.TryGetValue(kuralAdi, out var sablon))
            return sablon;
            
        return SistemMesajlari.GetValueOrDefault("Default", 
            "Security attack detected. Multiple failed attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}");
    }
} 