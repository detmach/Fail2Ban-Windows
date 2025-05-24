namespace Fail2Ban.Configuration;

/// <summary>
/// Fail2Ban ana konfigürasyon ayarları
/// </summary>
public class Fail2BanSettings
{
    /// <summary>
    /// İzin verilen maksimum hatalı giriş sayısı
    /// </summary>
    public int MaxHataliGiris { get; set; } = 3;
    
    /// <summary>
    /// Engelleme süresi (saniye cinsinden)
    /// </summary>
    public int EngellemeZamani { get; set; } = 18000; // 5 saat
    
    /// <summary>
    /// Log dosyası kontrol aralığı (milisaniye)
    /// </summary>
    public int KontrolAraligi { get; set; } = 10000; // 10 saniye
    
    /// <summary>
    /// Log dosya yolu şablonu
    /// </summary>
    public string LogDosyaYolSablonu { get; set; } = @"C:\Program Files (x86)\Mail Enable\Logging\SMTP\SMTP-Activity-{0}.log";
    
    /// <summary>
    /// Geçici log dosya adı
    /// </summary>
    public string GeciciLogDosyaAdi { get; set; } = "access.log";
    
    /// <summary>
    /// Log filtreleme kuralları
    /// </summary>
    public List<LogFilter> LogFiltreler { get; set; } = new();
}

/// <summary>
/// Log filtreleme kuralı
/// </summary>
public class LogFilter
{
    /// <summary>
    /// Filtrenin adı
    /// </summary>
    public string Ad { get; set; } = string.Empty;
    
    /// <summary>
    /// Regex pattern
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// IP adresini yakalayan grup adı
    /// </summary>
    public string IpGrupAdi { get; set; } = "ipAdresi";
    
    /// <summary>
    /// Bu filtrenin aktif olup olmadığı
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Bu filtre için özel maksimum hata sayısı (boşsa genel ayar kullanılır)
    /// </summary>
    public int? OzelMaxHata { get; set; }
    
    /// <summary>
    /// Bu filtre için özel engelleme süresi (boşsa genel ayar kullanılır)
    /// </summary>
    public int? OzelEngellemeSuresi { get; set; }
} 