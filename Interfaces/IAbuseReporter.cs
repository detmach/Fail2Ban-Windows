using Fail2Ban.Models;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Abuse raporlama servisi arayüzü
/// </summary>
public interface IAbuseReporter
{
    /// <summary>
    /// IP adresini AbuseIPDB'ye raporlar (eski metod - uyumluluk için)
    /// </summary>
    /// <param name="engellenenIp">Raporlanacak IP bilgileri</param>
    /// <returns>İşlem başarılı ise true</returns>
    [Obsolete("Bu metod artık kullanılmıyor. ReportBanAsync metodunu kullanın.")]
    Task<bool> ReportIpAsync(EngellenenIP engellenenIp);
    
    /// <summary>
    /// Ban kaydını AbuseIPDB'ye raporlar (yeni metod)
    /// </summary>
    /// <param name="banKaydi">Raporlanacak ban kaydı</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> ReportBanAsync(BanKaydi banKaydi);
    
    /// <summary>
    /// Servisin aktif olup olmadığını kontrol eder
    /// </summary>
    /// <returns>Aktif ise true</returns>
    bool IsEnabled();
} 