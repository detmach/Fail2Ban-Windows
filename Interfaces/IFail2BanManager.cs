using Fail2Ban.Models;

namespace Fail2Ban.Interfaces;

/// <summary>
/// Ana Fail2Ban yönetim servisi arayüzü
/// </summary>
public interface IFail2BanManager
{
    /// <summary>
    /// Hatalı giriş kaydeder
    /// </summary>
    /// <param name="ipAdresi">IP adresi</param>
    /// <param name="filterAdi">Filtre adı</param>
    /// <returns>IP engellenmiş ise true</returns>
    Task<bool> RecordFailedAttemptAsync(string ipAdresi, string filterAdi);
    
    /// <summary>
    /// IP adresini manuel olarak engeller
    /// </summary>
    /// <param name="ipAdresi">Engellenecek IP adresi</param>
    /// <param name="engellemeSuresi">Engelleme süresi (saniye)</param>
    /// <param name="sebep">Engelleme sebebi</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> BlockIpManuallyAsync(string ipAdresi, int engellemeSuresi, string sebep = "Manuel Engelleme");
    
    /// <summary>
    /// IP adresinin engellemesini manuel olarak kaldırır
    /// </summary>
    /// <param name="ipAdresi">Engeli kaldırılacak IP adresi</param>
    /// <returns>İşlem başarılı ise true</returns>
    Task<bool> UnblockIpManuallyAsync(string ipAdresi);
    
    /// <summary>
    /// Süresi dolan engelleri temizler
    /// </summary>
    /// <returns>Temizlenen IP sayısı</returns>
    Task<int> CleanupExpiredBlocksAsync();
    
    /// <summary>
    /// Aktif engellenen IP'leri döndürür
    /// </summary>
    /// <returns>Engellenmiş IP listesi</returns>
    List<EngellenenIP> GetBlockedIps();
    
    /// <summary>
    /// Hatalı giriş yapan IP'leri döndürür
    /// </summary>
    /// <returns>Hatalı giriş listesi</returns>
    List<HataliGiris> GetFailedAttempts();
    
    /// <summary>
    /// IP adresinin engelleme durumunu kontrol eder
    /// </summary>
    /// <param name="ipAdresi">Kontrol edilecek IP adresi</param>
    /// <returns>Engellenmiş IP bilgisi veya null</returns>
    EngellenenIP? GetBlockedIpInfo(string ipAdresi);
} 