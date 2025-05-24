using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;
using System.Xml;
using System.Net;
using System.Collections.Concurrent;

namespace Fail2Ban.Services;

/// <summary>
/// Windows Event Log izleme servisi
/// </summary>
public class WindowsEventLogMonitor : IEventLogMonitor, IDisposable
{
    private readonly ILogger<WindowsEventLogMonitor> _logger;
    private readonly IFail2BanManager _fail2BanManager;
    private readonly EventLogSettings _settings;
    private readonly List<EventLog> _eventLogs = new();
    private readonly Dictionary<object, string> _eventLogNames = new();
    private bool _isMonitoring = false;
    private readonly object _lockObject = new object();
    
    // Event processing duplicate prevention
    private readonly ConcurrentDictionary<string, DateTime> _processedEvents = new();
    private readonly TimeSpan _duplicateWindow = TimeSpan.FromSeconds(5); // 5 saniye içinde aynı event'i tekrar işleme

    public WindowsEventLogMonitor(
        ILogger<WindowsEventLogMonitor> logger,
        IOptions<EventLogSettings> settings,
        IFail2BanManager fail2BanManager)
    {
        _logger = logger;
        _settings = settings.Value;
        _fail2BanManager = fail2BanManager;
    }

    public bool IsMonitoring => _isMonitoring;

    public async Task StartMonitoringAsync()
    {
        if (!_settings.Aktif)
        {
            _logger.LogInformation("Event Log izleme devre dışı");
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Event Log izleme sadece Windows'da çalışır");
            return;
        }

        lock (_lockObject)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Event Log izleme zaten aktif");
                return;
            }

            try
            {
                // Her izlenen log için EventLog oluştur
                foreach (var logName in _settings.IzlenenLoglar)
                {
                    try
                    {
                        var eventLog = new EventLog(logName);
                        eventLog.EntryWritten += OnEventLogEntryWritten;
                        eventLog.EnableRaisingEvents = true;
                        _eventLogs.Add(eventLog);
                        
                        // EventLog instance'ını log adıyla eşleştir
                        _eventLogNames[eventLog] = logName;
                        
                        _logger.LogInformation("Event Log dinleniyor: {LogName}", logName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Event Log '{LogName}' dinlenemedi", logName);
                    }
                }
                
                _isMonitoring = true;
                _logger.LogInformation("Windows Event Log izleme başlatıldı - {LogCount} log dinleniyor", _eventLogs.Count);
                _logger.LogInformation("İzlenen Event ID'ler: {EventIds}", string.Join(", ", _settings.IzlenenEventIdler));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event Log izleme başlatılamadı");
                throw;
            }
        }

        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        lock (_lockObject)
        {
            if (!_isMonitoring)
                return;

            try
            {
                foreach (var eventLog in _eventLogs)
                {
                    try
                    {
                        eventLog.EnableRaisingEvents = false;
                        eventLog.EntryWritten -= OnEventLogEntryWritten;
                        _eventLogNames.Remove(eventLog);
                        eventLog.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Event Log kapatılırken hata");
                    }
                }
                
                _eventLogs.Clear();
                _eventLogNames.Clear();
                _isMonitoring = false;
                _logger.LogInformation("Windows Event Log izleme durduruldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event Log izleme durdurulurken hata");
            }
        }

        await Task.CompletedTask;
    }

    private async void OnEventLogEntryWritten(object sender, EntryWrittenEventArgs e)
    {
        try
        {
            var entry = e.Entry;
            
            // Sadece izlenen Event ID'leri işle
            if (!_settings.IzlenenEventIdler.Contains((int)entry.InstanceId))
                return;

            // Log adını güvenli bir şekilde al
            string logName = "Unknown";
            if (_eventLogNames.TryGetValue(sender, out var name))
            {
                logName = name;
            }
            else
            {
                // Fallback: EventLog olarak cast etmeye çalış
                try
                {
                    if (sender is EventLog eventLog)
                    {
                        logName = eventLog.Log;
                    }
                }
                catch (Exception castEx)
                {
                    _logger.LogDebug(castEx, "EventLog cast yapılamadı, varsayılan log adı kullanılıyor");
                }
            }

            await ProcessEventAsync((int)entry.InstanceId, entry.Message, entry.TimeGenerated, entry.Source, logName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event Log entry işlenirken hata");
        }
    }

    public async Task ProcessEventAsync(int eventId, string eventData, DateTime timestamp)
    {
        // Backward compatibility için overload
        await ProcessEventAsync(eventId, eventData, timestamp, "Unknown", "Unknown");
    }

    private async Task ProcessEventAsync(int eventId, string eventData, DateTime timestamp, string source, string logName)
    {
        _logger.LogDebug("Event işleniyor - ID: {EventId}, Log: {LogName}, Source: {Source}, Zaman: {Timestamp}", 
            eventId, logName, source, timestamp);

        // Duplicate event kontrolü - aynı event'in 5 saniye içinde tekrar işlenmesini engelle
        var eventKey = $"{eventId}_{timestamp:yyyy-MM-dd HH:mm:ss}_{source}_{eventData.Length}";
        var now = DateTime.Now;
        
        if (_processedEvents.TryGetValue(eventKey, out var lastProcessed) && 
            (now - lastProcessed) < _duplicateWindow)
        {
            _logger.LogDebug("Duplicate event tespit edildi, atlanıyor - Key: {EventKey}", eventKey);
            return;
        }
        
        // Event'i işlendiklerden kaydet
        _processedEvents.TryAdd(eventKey, now);
        
        // Eski event kayıtlarını temizle (10 dakikadan eski olanları)
        var oldKeys = _processedEvents.Where(kvp => (now - kvp.Value) > TimeSpan.FromMinutes(10))
                                     .Select(kvp => kvp.Key)
                                     .ToList();
        foreach (var oldKey in oldKeys)
        {
            _processedEvents.TryRemove(oldKey, out _);
        }

        switch (eventId)
        {
            case 4625: // An account failed to log on (Security Log)
                await ProcessFailedLogonEvent4625Async(eventData, timestamp);
                break;
            
            case 4771: // Kerberos pre-authentication failed (Security Log)
                await ProcessKerberosFailureAsync(eventData, timestamp);
                break;
                
            case 18456: // SQL Server login failed (Application Log)
                await ProcessSQLServerLoginFailureAsync(eventData, timestamp, source);
                break;
                
            default:
                _logger.LogDebug("İşlenmeyen Event ID: {EventId}", eventId);
                break;
        }
    }

    private async Task ProcessFailedLogonEvent4625Async(string eventData, DateTime timestamp)
    {
        try
        {
            // Event 4625 XML formatında gelir, IP ve kullanıcı adını çıkaralım
            var ipAddress = ExtractIpAddressFromEvent(eventData);
            var username = ExtractUsernameFromEvent(eventData);
            var logonType = ExtractLogonTypeFromEvent(eventData);

            if (!string.IsNullOrEmpty(ipAddress) && IsValidIpAddress(ipAddress))
            {
                await ProcessFailedLogonEventAsync(ipAddress, username, logonType, timestamp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event 4625 işlenirken hata");
        }
    }

    private async Task ProcessKerberosFailureAsync(string eventData, DateTime timestamp)
    {
        try
        {
            var ipAddress = ExtractIpAddressFromEvent(eventData);
            var username = ExtractUsernameFromEvent(eventData);

            if (!string.IsNullOrEmpty(ipAddress) && IsValidIpAddress(ipAddress))
            {
                _logger.LogDebug("Kerberos başarısız giriş - IP: {IpAddress}, Kullanıcı: {Username}", 
                    ipAddress, username);
                
                await _fail2BanManager.RecordFailedAttemptAsync(ipAddress, "EventLog-Kerberos");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kerberos event işlenirken hata");
        }
    }

    private async Task ProcessSQLServerLoginFailureAsync(string eventData, DateTime timestamp, string source)
    {
        try
        {
            // SQL Server event'ini sadece MSSQLSERVER source'undan kabul et
            if (!source.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("SQL Server event'i farklı kaynaktan geldi: {Source}", source);
                return;
            }

            var ipAddress = ExtractSQLServerIpFromEvent(eventData);
            var username = ExtractSQLServerUsernameFromEvent(eventData);

            if (!string.IsNullOrEmpty(ipAddress) && IsValidIpAddress(ipAddress))
            {
                _logger.LogDebug("SQL Server başarısız giriş - IP: {IpAddress}, Kullanıcı: {Username}, Event: {EventData}", 
                    ipAddress, username, eventData.Substring(0, Math.Min(200, eventData.Length)));
                
                await _fail2BanManager.RecordFailedAttemptAsync(ipAddress, "EventLog-SQLServer");
            }
            else
            {
                _logger.LogDebug("SQL Server event'inden IP adresi çıkarılamadı: {EventData}", 
                    eventData.Substring(0, Math.Min(200, eventData.Length)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server event işlenirken hata");
        }
    }

    public async Task ProcessFailedLogonEventAsync(string ipAddress, string username, string logonType, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return;

        _logger.LogDebug("Başarısız giriş eventi - IP: {IpAddress}, Kullanıcı: {Username}, Logon Type: {LogonType}", 
            ipAddress, username, logonType);

        // Logon Type 10 = RDP, Logon Type 3 = Network
        var filterName = logonType switch
        {
            "10" => "EventLog-RDP",
            "3" => "EventLog-Network",
            _ => "EventLog-Other"
        };

        // Fail2Ban'e kaydet
        await _fail2BanManager.RecordFailedAttemptAsync(ipAddress, filterName);
    }

    private string ExtractSQLServerIpFromEvent(string eventData)
    {
        // SQL Server event formatı: "Login failed for user 'username'. Reason: ... [CLIENT: IP_ADDRESS]"
        var patterns = new[]
        {
            @"\[CLIENT:\s*([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\]",
            @"CLIENT:\s*([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})",
            @"from\s+([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(eventData, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var ip = match.Groups[1].Value;
                if (IsValidIpAddress(ip) && !IsLocalIpAddress(ip))
                    return ip;
            }
        }

        return string.Empty;
    }

    private string ExtractSQLServerUsernameFromEvent(string eventData)
    {
        // SQL Server format: "Login failed for user 'username'."
        var patterns = new[]
        {
            @"Login failed for user '([^']+)'",
            @"Login failed for user\s+([^\s\.]+)",
            @"user\s+'([^']+)'",
            @"user\s+([^\s\.]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(eventData, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return "Bilinmeyen";
    }

    private string ExtractIpAddressFromEvent(string eventData)
    {
        // Event 4625 ve 4771'den IP adresini çıkar
        var patterns = new[]
        {
            @"Source Network Address:\s*([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})",
            @"Client Address:\s*([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})",
            @"IP Address:\s*([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})",
            @"(\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(eventData, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var ip = match.Groups[1].Value;
                if (IsValidIpAddress(ip) && !IsLocalIpAddress(ip))
                    return ip;
            }
        }

        return string.Empty;
    }

    private string ExtractUsernameFromEvent(string eventData)
    {
        var patterns = new[]
        {
            @"Account Name:\s*([^\r\n\t]+)",
            @"User Name:\s*([^\r\n\t]+)",
            @"Target User Name:\s*([^\r\n\t]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(eventData, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return "Bilinmeyen";
    }

    private string ExtractLogonTypeFromEvent(string eventData)
    {
        var match = Regex.Match(eventData, @"Logon Type:\s*([0-9]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "0";
    }

    private bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    private bool IsLocalIpAddress(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return true;

        // Yerel IP aralıklarını filtrele
        var bytes = ip.GetAddressBytes();
        
        return ip.Equals(IPAddress.Loopback) ||
               ip.Equals(IPAddress.Any) ||
               (bytes[0] == 127) || // 127.x.x.x
               (bytes[0] == 10) ||  // 10.x.x.x
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.x.x - 172.31.x.x
               (bytes[0] == 192 && bytes[1] == 168) || // 192.168.x.x
               (bytes[0] == 169 && bytes[1] == 254);   // 169.254.x.x (APIPA)
    }

    public void Dispose()
    {
        StopMonitoringAsync().Wait();
    }
} 