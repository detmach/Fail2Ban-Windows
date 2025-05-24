namespace Fail2Ban.Configuration;

/// <summary>
/// Windows Event Log izleme ayarları
/// </summary>
public class EventLogSettings
{
    /// <summary>
    /// Event Log izleme aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// İzlenecek Event ID'lerin listesi
    /// </summary>
    public List<int> IzlenenEventIdler { get; set; } = new()
    {
        4625, // An account failed to log on (Security)
        4771, // Kerberos pre-authentication failed (Security)
        18456 // SQL Server login failed (Application)
    };
    
    /// <summary>
    /// İzlenecek Event Log'ların listesi
    /// </summary>
    public List<string> IzlenenLoglar { get; set; } = new()
    {
        "Security",     // Windows güvenlik olayları
        "Application"   // Uygulama olayları (SQL Server vs.)
    };
    
    /// <summary>
    /// Event Log filtreler için özel ayarlar
    /// </summary>
    public List<EventLogFilter> Filtreler { get; set; } = new();
}

/// <summary>
/// Event Log filtre ayarları
/// </summary>
public class EventLogFilter
{
    /// <summary>
    /// Filter adı (EventLog-RDP, EventLog-Network, EventLog-SQLServer vb.)
    /// </summary>
    public required string Ad { get; set; }
    
    /// <summary>
    /// Bu filtre aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Maksimum hatalı giriş sayısı (null ise genel ayar kullanılır)
    /// </summary>
    public int? OzelMaxHata { get; set; }
    
    /// <summary>
    /// Engelleme süresi saniye cinsinden (null ise genel ayar kullanılır)
    /// </summary>
    public int? OzelEngellemeSuresi { get; set; }
    
    /// <summary>
    /// İzlenecek Logon Type'lar (RDP için 10, Network için 3)
    /// </summary>
    public List<int> LogonTypes { get; set; } = new();
    
    /// <summary>
    /// Hangi Event Log'dan geldiği (Security, Application vs.)
    /// </summary>
    public string? EventLogKaynagi { get; set; }
    
    /// <summary>
    /// Event Source (MSSQLSERVER vs.)
    /// </summary>
    public string? EventSource { get; set; }
    
    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Aciklama { get; set; }
} 