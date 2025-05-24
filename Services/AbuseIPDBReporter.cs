using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;
using Fail2Ban.Models;

namespace Fail2Ban.Services;

/// <summary>
/// AbuseIPDB raporlama servisi implementasyonu
/// </summary>
public class AbuseIPDBReporter : IAbuseReporter
{
    private readonly ILogger<AbuseIPDBReporter> _logger;
    private readonly AbuseIPDBSettings _settings;
    private readonly HttpClient _httpClient;

    public AbuseIPDBReporter(ILogger<AbuseIPDBReporter> logger, IOptions<AbuseIPDBSettings> settings, HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
        
        ConfigureHttpClient();
    }

    /// <summary>
    /// HTTP istemcisini yapılandırır
    /// </summary>
    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }
    }

    public bool IsEnabled()
    {
        return _settings.Aktif && !string.IsNullOrWhiteSpace(_settings.ApiKey);
    }

    public async Task<bool> ReportIpAsync(EngellenenIP engellenenIp)
    {
        if (!IsEnabled())
        {
            _logger.LogDebug("AbuseIPDB raporlaması devre dışı");
            return false;
        }

        try
        {
            var comment = string.Format(_settings.RaporSablonu, 
                engellenenIp.EngellemeDAkika, 
                engellenenIp.EngellenmeTarihi.ToString("dddd, dd MMMM yyyy HH:mm"));

            var requestBody = $"ip={engellenenIp.IpAdresi}&categories={_settings.Kategori}&comment={Uri.EscapeDataString(comment)}";
            
            _logger.LogDebug("AbuseIPDB'ye rapor gönderiliyor: {IpAdresi}", engellenenIp.IpAdresi);
            
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.PostAsync(_settings.ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("AbuseIPDB'ye başarıyla rapor gönderildi: {IpAdresi}", engellenenIp.IpAdresi);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AbuseIPDB raporu başarısız - IP: {IpAdresi}, Status: {StatusCode}, Error: {Error}", 
                    engellenenIp.IpAdresi, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken ağ hatası oluştu: {IpAdresi}", engellenenIp.IpAdresi);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken zaman aşımı oluştu: {IpAdresi}", engellenenIp.IpAdresi);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken beklenmedik hata oluştu: {IpAdresi}", engellenenIp.IpAdresi);
            return false;
        }
    }
} 