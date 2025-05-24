namespace Fail2Ban.Models;

/// <summary>
/// Hatalı giriş bilgilerini temsil eder
/// </summary>
public class HataliGiris
{
    /// <summary>
    /// IP adresi
    /// </summary>
    public string IpAdresi { get; set; } = string.Empty;
    
    /// <summary>
    /// Hata sayısı
    /// </summary>
    public int HataSayisi { get; set; }
    
    /// <summary>
    /// İlk hata tarihi
    /// </summary>
    public DateTime IlkHataTarihi { get; set; }
    
    /// <summary>
    /// Son hata tarihi
    /// </summary>
    public DateTime SonHataTarihi { get; set; }
    
    /// <summary>
    /// Hangi filtre tarafından yakalandığı
    /// </summary>
    public string FilterAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Hata sayısını bir artır
    /// </summary>
    public void HataArtir()
    {
        HataSayisi++;
        SonHataTarihi = DateTime.Now;
        
        if (HataSayisi == 1)
        {
            IlkHataTarihi = DateTime.Now;
        }
    }
} 