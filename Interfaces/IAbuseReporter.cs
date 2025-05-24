using Fail2Ban.Models;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Abuse raporlama servisi arayüzü
/// </summary>
public interface IAbuseReporter
{
    /// <summary>
    /// IP adresini AbuseIPDB'ye raporlar
    /// </summary>
    /// <param name="engellenenIp">Raporlanacak IP bilgileri</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> ReportIpAsync(EngellenenIP engellenenIp);
    
    /// <summary>
    /// Servisin aktif olup olmadığını kontrol eder
    /// </summary>
    /// <returns>Aktif ise true</returns>
    bool IsEnabled();
} 