using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;

namespace Fail2Ban.Services;

/// <summary>
/// Log dosyasını izleyen background servis
/// </summary>
public class LogMonitorService : BackgroundService
{
    private readonly ILogger<LogMonitorService> _logger;
    private readonly Fail2BanSettings _settings;
    private readonly ILogAnalyzer _logAnalyzer;
    private readonly IFail2BanManager _fail2BanManager;
    private long _sonOkunanSatirIndeksi = 0;

    public LogMonitorService(
        ILogger<LogMonitorService> logger,
        IOptions<Fail2BanSettings> settings,
        ILogAnalyzer logAnalyzer,
        IFail2BanManager fail2BanManager)
    {
        _logger = logger;
        _settings = settings.Value;
        _logAnalyzer = logAnalyzer;
        _fail2BanManager = fail2BanManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log izleme servisi başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessLogFileAsync();
                await CleanupExpiredBlocksAsync();
                
                await Task.Delay(_settings.KontrolAraligi, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Log izleme servisi durduruldu");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log izleme döngüsünde hata oluştu");
                await Task.Delay(5000, stoppingToken); // 5 saniye bekle ve tekrar dene
            }
        }
    }

    /// <summary>
    /// Log dosyasını işler
    /// </summary>
    private async Task ProcessLogFileAsync()
    {
        var logFilePath = GetCurrentLogFilePath();
        
        if (!File.Exists(logFilePath))
        {
            _logger.LogDebug("Log dosyası bulunamadı: {FilePath}", logFilePath);
            return;
        }

        try
        {
            // Log dosyasını geçici bir dosyaya kopyala
            var tempFilePath = Path.Combine(Path.GetTempPath(), _settings.GeciciLogDosyaAdi);
            File.Copy(logFilePath, tempFilePath, true);

            var logLines = await File.ReadAllLinesAsync(tempFilePath);
            
            // Sadece yeni satırları işle
            for (var i = _sonOkunanSatirIndeksi; i < logLines.Length; i++)
            {
                await ProcessLogLineAsync(logLines[i]);
            }

            _sonOkunanSatirIndeksi = logLines.Length;
            
            // Geçici dosyayı sil
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Log dosyası okuma hatası: {FilePath}", logFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log dosyası işlenirken beklenmedik hata oluştu: {FilePath}", logFilePath);
        }
    }

    /// <summary>
    /// Tek bir log satırını işler
    /// </summary>
    private async Task ProcessLogLineAsync(string logLine)
    {
        if (string.IsNullOrWhiteSpace(logLine))
            return;

        try
        {
            var (ipAdresi, filterAdi) = _logAnalyzer.AnalyzeWithAllFilters(logLine);
            
            if (!string.IsNullOrEmpty(ipAdresi) && !string.IsNullOrEmpty(filterAdi))
            {
                var blocked = await _fail2BanManager.RecordFailedAttemptAsync(ipAdresi, filterAdi);
                
                if (blocked)
                {
                    _logger.LogWarning("Yeni IP engellemesi: {IpAdresi} - Filtre: {FilterAdi}", ipAdresi, filterAdi);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log satırı işlenirken hata oluştu: {LogLine}", 
                logLine.Length > 100 ? logLine.Substring(0, 100) + "..." : logLine);
        }
    }

    /// <summary>
    /// Süresi dolan engelleri temizler
    /// </summary>
    private async Task CleanupExpiredBlocksAsync()
    {
        try
        {
            var cleanedCount = await _fail2BanManager.CleanupExpiredBlocksAsync();
            
            if (cleanedCount > 0)
            {
                _logger.LogInformation("Süresi dolan {Count} IP engellemesi temizlendi", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Süresi dolan engeller temizlenirken hata oluştu");
        }
    }

    /// <summary>
    /// Güncel log dosya yolunu döndürür
    /// </summary>
    private string GetCurrentLogFilePath()
    {
        var today = DateTime.Today;
        var formattedDate = $"{today.Year % 100:D2}{today.Month:D2}{today.Day:D2}";
        
        return string.Format(_settings.LogDosyaYolSablonu, formattedDate);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Log izleme servisi durduruluyor...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Log izleme servisi durduruldu");
    }
} 