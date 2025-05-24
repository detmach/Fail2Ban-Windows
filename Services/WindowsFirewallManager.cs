using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Fail2Ban.Interfaces;

namespace Fail2Ban.Services;

/// <summary>
/// Windows Firewall yönetim servisi implementasyonu
/// </summary>
public class WindowsFirewallManager : IFirewallManager
{
    private readonly ILogger<WindowsFirewallManager> _logger;
    private const string RULE_PREFIX = "Fail2Ban_";

    public WindowsFirewallManager(ILogger<WindowsFirewallManager> logger)
    {
        _logger = logger;
    }

    public async Task<bool> BlockIpAsync(string ipAdresi)
    {
        try
        {
            var ruleName = $"{RULE_PREFIX}{ipAdresi}";
            var command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=in interface=any action=block remoteip={ipAdresi}";
            
            var result = await ExecuteCommandAsync(command);
            
            if (result.Success)
            {
                _logger.LogInformation("IP adresi başarıyla engellendi: {IpAdresi}", ipAdresi);
                return true;
            }
            else
            {
                _logger.LogError("IP adresi engellenirken hata oluştu: {IpAdresi} - {Error}", ipAdresi, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi engellenirken beklenmedik hata oluştu: {IpAdresi}", ipAdresi);
            return false;
        }
    }

    public async Task<bool> UnblockIpAsync(string ipAdresi)
    {
        try
        {
            var ruleName = $"{RULE_PREFIX}{ipAdresi}";
            var command = $"netsh advfirewall firewall delete rule name=\"{ruleName}\"";
            
            var result = await ExecuteCommandAsync(command);
            
            if (result.Success)
            {
                _logger.LogInformation("IP adresinin engellemesi başarıyla kaldırıldı: {IpAdresi}", ipAdresi);
                return true;
            }
            else
            {
                _logger.LogWarning("IP adresinin engellemesi kaldırılırken hata oluştu: {IpAdresi} - {Error}", ipAdresi, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresinin engellemesi kaldırılırken beklenmedik hata oluştu: {IpAdresi}", ipAdresi);
            return false;
        }
    }

    public async Task<bool> IsBlockedAsync(string ipAdresi)
    {
        try
        {
            var ruleName = $"{RULE_PREFIX}{ipAdresi}";
            var command = $"netsh advfirewall firewall show rule name=\"{ruleName}\"";
            
            var result = await ExecuteCommandAsync(command);
            
            // Kural bulunursa output'ta "Rule Name:" ifadesi olacaktır
            return result.Success && result.Output.Contains("Rule Name:");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP adresi engelleme durumu kontrol edilirken hata oluştu: {IpAdresi}", ipAdresi);
            return false;
        }
    }

    public async Task<List<string>> GetBlockedIpsAsync()
    {
        var blockedIps = new List<string>();
        
        try
        {
            var command = "netsh advfirewall firewall show rule name=all";
            var result = await ExecuteCommandAsync(command);
            
            if (result.Success)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.Contains("Rule Name:") && line.Contains(RULE_PREFIX))
                    {
                        // "Fail2Ban_192.168.1.100" formatından IP'yi çıkar
                        var startIndex = line.IndexOf(RULE_PREFIX) + RULE_PREFIX.Length;
                        var endIndex = line.Length;
                        
                        if (startIndex < endIndex)
                        {
                            var ipPart = line.Substring(startIndex).Trim();
                            // Satır sonundaki gereksiz karakterleri temizle
                            ipPart = ipPart.Split(' ')[0].Split('\r')[0];
                            
                            if (!string.IsNullOrWhiteSpace(ipPart))
                            {
                                blockedIps.Add(ipPart);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Engellenmiş IP'ler listelenirken hata oluştu");
        }
        
        return blockedIps;
    }

    /// <summary>
    /// Windows komutunu asenkron olarak çalıştırır
    /// </summary>
    /// <param name="command">Çalıştırılacak komut</param>
    /// <returns>Komut sonucu</returns>
    private async Task<CommandResult> ExecuteCommandAsync(string command)
    {
        try
        {
            using var process = new Process();
            
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            
            _logger.LogDebug("Komut çalıştırılıyor: {Command}", command);
            
            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;
            
            var success = process.ExitCode == 0;
            
            _logger.LogDebug("Komut tamamlandı - ExitCode: {ExitCode}, Success: {Success}", 
                process.ExitCode, success);
            
            return new CommandResult(success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Komut çalıştırılırken hata oluştu: {Command}", command);
            return new CommandResult(false, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Komut sonucu
    /// </summary>
    private record CommandResult(bool Success, string Output, string Error);
} 