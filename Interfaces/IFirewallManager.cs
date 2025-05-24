namespace Fail2Ban.Interfaces;

/// <summary>
/// Firewall yönetim servisi arayüzü
/// </summary>
public interface IFirewallManager
{
    /// <summary>
    /// IP adresini firewall'da engeller
    /// </summary>
    /// <param name="ipAdresi">Engellenecek IP adresi</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> BlockIpAsync(string ipAdresi);
    
    /// <summary>
    /// IP adresinin engellemesini kaldırır
    /// </summary>
    /// <param name="ipAdresi">Engeli kaldırılacak IP adresi</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> UnblockIpAsync(string ipAdresi);
    
    /// <summary>
    /// IP adresinin engellenip engellenmediğini kontrol eder
    /// </summary>
    /// <param name="ipAdresi">Kontrol edilecek IP adresi</param>
    /// <returns>Engellenmiş ise true</returns>
    Task<bool> IsBlockedAsync(string ipAdresi);
    
    /// <summary>
    /// Tüm Fail2Ban kurallarını listeler
    /// </summary>
    /// <returns>Engellenmiş IP adreslerinin listesi</returns>
    Task<List<string>> GetBlockedIpsAsync();
} 