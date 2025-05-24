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

    [Obsolete("Bu metod artık kullanılmıyor. ReportBanAsync metodunu kullanın.")]
    public async Task<bool> ReportIpAsync(EngellenenIP engellenenIp)
    {
        // Eski uyumluluk için, varsayılan sistemi kullan
        var banKaydi = new BanKaydi
        {
            IpAdresi = engellenenIp.IpAdresi,
            YasaklamaZamani = engellenenIp.EngellenmeTarihi,
            BanSuresiDakika = engellenenIp.EngellemeDAkika,
            KuralAdi = "Default",
            BasarisizGirisSayisi = 1
        };
        
        return await ReportBanAsync(banKaydi);
    }

    /// <summary>
    /// Ban kaydını AbuseIPDB'ye raporlar (yeni metod)
    /// </summary>
    public async Task<bool> ReportBanAsync(BanKaydi banKaydi)
    {
        if (!IsEnabled())
        {
            _logger.LogDebug("AbuseIPDB raporlaması devre dışı");
            return false;
        }

        try
        {
            // Ban sistemine göre uygun mesaj şablonunu al
            var mesajSablonu = _settings.GetMesajSablonu(banKaydi.KuralAdi);
            
            // Mesajı formatla
            var comment = string.Format(mesajSablonu,
                banKaydi.IpAdresi,                                           // {0} IP adresi
                banKaydi.BanSuresiDakika,                                    // {1} Ban süresi dakika
                banKaydi.YasaklamaZamani.ToString("dddd, dd MMMM yyyy HH:mm"), // {2} Ban tarihi
                banKaydi.BasarisizGirisSayisi                                // {3} Başarısız giriş sayısı
            );

            var requestBody = $"ip={banKaydi.IpAdresi}&categories={_settings.Kategori}&comment={Uri.EscapeDataString(comment)}";
            
            _logger.LogDebug("AbuseIPDB'ye rapor gönderiliyor - IP: {IpAdresi}, Kural: {KuralAdi}, Mesaj: {Mesaj}", 
                banKaydi.IpAdresi, banKaydi.KuralAdi, comment);
            
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.PostAsync(_settings.ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("AbuseIPDB'ye başarıyla rapor gönderildi - IP: {IpAdresi}, Kural: {KuralAdi}", 
                    banKaydi.IpAdresi, banKaydi.KuralAdi);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("AbuseIPDB raporu başarısız - IP: {IpAdresi}, Kural: {KuralAdi}, Status: {StatusCode}, Error: {Error}", 
                    banKaydi.IpAdresi, banKaydi.KuralAdi, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken ağ hatası oluştu - IP: {IpAdresi}, Kural: {KuralAdi}", 
                banKaydi.IpAdresi, banKaydi.KuralAdi);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken zaman aşımı oluştu - IP: {IpAdresi}, Kural: {KuralAdi}", 
                banKaydi.IpAdresi, banKaydi.KuralAdi);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AbuseIPDB raporu gönderilirken beklenmedik hata oluştu - IP: {IpAdresi}, Kural: {KuralAdi}", 
                banKaydi.IpAdresi, banKaydi.KuralAdi);
            return false;
        }
    }
} 