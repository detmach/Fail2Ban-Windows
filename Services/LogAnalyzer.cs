using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;

namespace Fail2Ban.Services;

/// <summary>
/// Log analiz servisi implementasyonu
/// </summary>
public class LogAnalyzer : ILogAnalyzer
{
    private readonly ILogger<LogAnalyzer> _logger;
    private readonly Fail2BanSettings _settings;
    private readonly Dictionary<string, Regex> _compiledRegexes;

    public LogAnalyzer(ILogger<LogAnalyzer> logger, IOptions<Fail2BanSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _compiledRegexes = new Dictionary<string, Regex>();
        
        // Regex'leri önceden derle
        CompileRegexPatterns();
    }

    /// <summary>
    /// Tüm regex pattern'lerini önceden derler
    /// </summary>
    private void CompileRegexPatterns()
    {
        foreach (var filter in _settings.LogFiltreler.Where(f => f.Aktif))
        {
            try
            {
                var regex = new Regex(filter.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                _compiledRegexes[filter.Ad] = regex;
                _logger.LogInformation("Regex pattern derlendi: {FilterAdi}", filter.Ad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Regex pattern derlenirken hata oluştu: {FilterAdi} - {Pattern}", 
                    filter.Ad, filter.Pattern);
            }
        }
    }

    public string? AnalyzeLogLine(string logSatiri, LogFilter filter)
    {
        if (string.IsNullOrWhiteSpace(logSatiri) || !filter.Aktif)
            return null;

        try
        {
            if (!_compiledRegexes.TryGetValue(filter.Ad, out var regex))
            {
                _logger.LogWarning("Filtre için derlenmiş regex bulunamadı: {FilterAdi}", filter.Ad);
                return null;
            }

            var match = regex.Match(logSatiri);
            if (match.Success && match.Groups[filter.IpGrupAdi].Success)
            {
                var ipAdresi = match.Groups[filter.IpGrupAdi].Value;
                
                if (IsValidIpAddress(ipAdresi))
                {
                    _logger.LogDebug("Şüpheli aktivite tespit edildi - IP: {IpAdresi}, Filtre: {FilterAdi}", 
                        ipAdresi, filter.Ad);
                    return ipAdresi;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log satırı analiz edilirken hata oluştu: {FilterAdi}", filter.Ad);
        }

        return null;
    }

    public (string? IpAdresi, string? FilterAdi) AnalyzeWithAllFilters(string logSatiri)
    {
        if (string.IsNullOrWhiteSpace(logSatiri))
            return (null, null);

        foreach (var filter in _settings.LogFiltreler.Where(f => f.Aktif))
        {
            var ipAdresi = AnalyzeLogLine(logSatiri, filter);
            if (!string.IsNullOrEmpty(ipAdresi))
            {
                return (ipAdresi, filter.Ad);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// IP adresinin geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="ipAdresi">Kontrol edilecek IP adresi</param>
    /// <returns>Geçerli ise true</returns>
    private static bool IsValidIpAddress(string ipAdresi)
    {
        if (string.IsNullOrWhiteSpace(ipAdresi))
            return false;

        // Basit IP adresi formatı kontrolü
        var parts = ipAdresi.Split('.');
        if (parts.Length != 4)
            return false;

        return parts.All(part => 
            int.TryParse(part, out var num) && num >= 0 && num <= 255);
    }
} 