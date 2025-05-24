namespace Fail2Ban.Configuration;

/// <summary>
/// Ban sistemlerinin aktif/pasif durumunu belirten ayarlar
/// </summary>
public class BanSistemleriSettings
{
    /// <summary>
    /// Log dosyası izleme sistemi
    /// </summary>
    public SistemAyari LogIzleme { get; set; } = new() { Aktif = true };
    
    /// <summary>
    /// Event Log izleme sistemi
    /// </summary>
    public SistemAyari EventLogIzleme { get; set; } = new() { Aktif = true };
    
    /// <summary>
    /// AbuseIPDB raporlama sistemi
    /// </summary>
    public SistemAyari AbuseIPDBRapor { get; set; } = new() { Aktif = true };
    
    /// <summary>
    /// Windows Firewall ban sistemi
    /// </summary>
    public SistemAyari WindowsFirewall { get; set; } = new() { Aktif = true };
}

/// <summary>
/// Sistem ayarı base sınıfı
/// </summary>
public class SistemAyari
{
    /// <summary>
    /// Sistem aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Sistem açıklaması
    /// </summary>
    public string? Aciklama { get; set; }
} 