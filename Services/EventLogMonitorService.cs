using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fail2Ban.Interfaces;

namespace Fail2Ban.Services;

/// <summary>
/// Event Log izleme background servisi
/// </summary>
public class EventLogMonitorService : BackgroundService
{
    private readonly ILogger<EventLogMonitorService> _logger;
    private readonly IEventLogMonitor _eventLogMonitor;

    public EventLogMonitorService(
        ILogger<EventLogMonitorService> logger,
        IEventLogMonitor eventLogMonitor)
    {
        _logger = logger;
        _eventLogMonitor = eventLogMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Event Log izleme servisi başlatılıyor...");
            
            await _eventLogMonitor.StartMonitoringAsync();
            
            // Servis çalışırken bekle
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event Log izleme servisi durduruldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event Log izleme servisinde hata");
        }
        finally
        {
            await _eventLogMonitor.StopMonitoringAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Event Log izleme servisi durduruluyor...");
        await _eventLogMonitor.StopMonitoringAsync();
        await base.StopAsync(cancellationToken);
    }
} 