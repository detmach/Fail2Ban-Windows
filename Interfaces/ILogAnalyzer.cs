using Fail2Ban.Configuration;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Log analiz servisi arayüzü
/// </summary>
public interface ILogAnalyzer
{
    /// <summary>
    /// Log satırını analiz eder ve şüpheli IP adresi döndürür
    /// </summary>
    /// <param name="logSatiri">Analiz edilecek log satırı</param>
    /// <param name="filter">Kullanılacak filtre</param>
    /// <returns>Şüpheli IP adresi veya null</returns>
    string? AnalyzeLogLine(string logSatiri, LogFilter filter);
    
    /// <summary>
    /// Tüm aktif filtreler ile log satırını analiz eder
    /// </summary>
    /// <param name="logSatiri">Analiz edilecek log satırı</param>
    /// <returns>IP adresi ve filtre adı tuple'ı</returns>
    (string? IpAdresi, string? FilterAdi) AnalyzeWithAllFilters(string logSatiri);
} 