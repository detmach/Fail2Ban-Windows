using Fail2Ban.Models;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Windows Event Log izleme interface'i
/// </summary>
public interface IEventLogMonitor
{
    /// <summary>
    /// Event log izlemeyi başlat
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Event log izlemeyi durdur
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Belirli bir event'i işle
    /// </summary>
    Task ProcessEventAsync(int eventId, string eventData, DateTime timestamp);
    
    /// <summary>
    /// Event log izleme aktif mi?
    /// </summary>
    bool IsMonitoring { get; }
    
    /// <summary>
    /// Event ID 4625 (başarısız giriş) olayını işle
    /// </summary>
    Task ProcessFailedLogonEventAsync(string ipAddress, string username, string logonType, DateTime timestamp);
} 