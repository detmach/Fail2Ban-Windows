# Fail2Ban Windows Servisi (.NET 9)

Windows platformu için geliştirilmiş modern Fail2Ban implementasyonu. Bu uygulama, log dosyalarını ve Windows Event Log'larını izleyerek şüpheli aktiviteleri tespit eder, SQLite veritabanında kalıcı ban kayıtları tutar ve Windows Firewall kullanarak otomatik IP engelleme işlemleri gerçekleştirir.

## 🚀 Yeni Özellikler (v1.0.2)

- **🗄️ SQLite Veritabanı Entegrasyonu**: Kalıcı ban kayıtları, program yeniden başlayınca aynı IP'lerin tekrar banlanmasını önler
- **📝 Windows Event Log İzleme**: Security ve Application log'larından gerçek zamanlı saldırı tespit
- **🛡️ Çoklu Saldırı Türü Desteği**: RDP, SMTP, Network, Kerberos, SQL Server saldırılarını tespit eder
- **🌐 AbuseIPDB Duplicate Kontrolü**: Aynı IP'lerin 24 saat içinde tekrar raporlanmasını önler
- **🎯 Sistem-Specific Mesajlar**: Her saldırı türü için özel AbuseIPDB mesajları
- **⚡ Thread Safety**: Çoklu thread desteği ile performans optimizasyonu
- **📊 Gelişmiş İstatistikler**: Detaylı ban istatistikleri ve raporlama

## 🎯 Desteklenen Saldırı Türleri

### Log Dosyası Tabanlı
- **SMTP-AUTH-Failed**: Mail Enable SMTP Authentication saldırıları
- **SMTP-Brute-Force**: SMTP Brute Force saldırıları

### Windows Event Log Tabanlı
- **EventLog-RDP**: RDP Brute Force (Event ID 4625, Logon Type 10)
- **EventLog-Network**: Network Authentication (Event ID 4625, Logon Type 3)
- **EventLog-Kerberos**: Kerberos Failure (Event ID 4771)
- **EventLog-SQLServer**: SQL Server Failed Login (Event ID 18456)
- **EventLog-Other**: Diğer Windows authentication hataları

## 📋 Gereksinimler

- Windows Server 2016+ / Windows 10+
- .NET 9.0 Runtime
- Yönetici (Administrator) yetkileri
- SQLite desteği (otomatik olarak dahil)
- İsteğe bağlı: Mail Enable SMTP Server

## 🛠️ Kurulum

### 1. Projeyi İndirin
```bash
git clone https://github.com/your-repo/fail2ban-windows.git
cd fail2ban-windows/Fail2Ban
```

### 2. Bağımlılıkları Yükleyin
```bash
dotnet restore
```

### 3. Projeyi Derleyin
```bash
dotnet build --configuration Release
```

### 4. Konfigürasyonu Düzenleyin
`appsettings.json` dosyasını ihtiyaçlarınıza göre düzenleyin.

## ⚙️ Konfigürasyon

### Ana Ayarlar (`Fail2BanSettings`)
```json
{
  "Fail2BanSettings": {
    "MaxHataliGiris": 3,
    "EngellemeZamani": 18000,
    "KontrolAraligi": 10000,
    "LogDosyaYolSablonu": "C:\\Program Files (x86)\\Mail Enable\\Logging\\SMTP\\SMTP-Activity-{0}.log",
    "LogFiltreler": [
      {
        "Ad": "SMTP-AUTH-Failed",
        "Pattern": "^(?<tarih>\\d{2}/\\d{2}/\\d{2} \\d{2}:\\d{2}:\\d{2})\\s+SMTP-IN\\s+\\w+\\.\\w+\\s+\\d+\\s+(?<ipAdresi>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})\\s+AUTH\\s+(?=.*535 Invalid Username or Password)",
        "IpGrupAdi": "ipAdresi",
        "Aktif": true,
        "OzelMaxHata": null,
        "OzelEngellemeSuresi": null
      }
    ]
  }
}
```

### Veritabanı Ayarları
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=fail2ban.db"
  }
}
```

### Event Log Ayarları
```json
{
  "EventLogSettings": {
    "Aktif": true,
    "IzlenenEventIdler": [4625, 4771, 18456],
    "IzlenenLoglar": ["Security", "Application"],
    "Filtreler": [
      {
        "Ad": "EventLog-RDP",
        "Aktif": true,
        "OzelMaxHata": 3,
        "OzelEngellemeSuresi": 3600,
        "LogonTypes": [10],
        "EventLogKaynagi": "Security",
        "Aciklama": "Uzak Masaüstü (RDP) başarısız giriş denemeleri"
      },
      {
        "Ad": "EventLog-SQLServer",
        "Aktif": true,
        "OzelMaxHata": 5,
        "OzelEngellemeSuresi": 3600,
        "EventLogKaynagi": "Application",
        "EventSource": "MSSQLSERVER",
        "Aciklama": "SQL Server başarısız giriş denemeleri"
      }
    ]
  }
}
```

### AbuseIPDB Ayarları (Duplicate Kontrolü ile)
```json
{
  "AbuseIPDBSettings": {
    "ApiKey": "your-api-key-here",
    "ApiUrl": "https://api.abuseipdb.com/api/v2/report",
    "Kategori": 18,
    "Aktif": true,
    "MinRaporAraligiSaat": 24,
    "SistemMesajlari": {
      "SMTP-AUTH-Failed": "SMTP Authentication attack detected. Multiple failed login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
      "EventLog-RDP": "RDP Brute Force attack detected. Multiple failed Remote Desktop login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}",
      "EventLog-SQLServer": "SQL Server Brute Force attack detected. Multiple failed database login attempts from {0}. IP banned for {1} minutes at {2}. Failed attempts: {3}"
    }
  }
}
```

### Ban Sistemleri Kontrolü
```json
{
  "BanSistemleri": {
    "LogIzleme": { "Aktif": true, "Aciklama": "Log dosyalarını izleyerek SMTP saldırılarını tespit eder" },
    "EventLogIzleme": { "Aktif": true, "Aciklama": "Windows Event Log'larını izleyerek RDP ve diğer saldırıları tespit eder" },
    "AbuseIPDBRapor": { "Aktif": true, "Aciklama": "Engellenen IP'leri AbuseIPDB'ye raporlar" },
    "WindowsFirewall": { "Aktif": true, "Aciklama": "Windows Firewall üzerinden IP engellemesi yapar" }
  }
}
```

## 🚦 Kullanım

### Konsol Modunda Çalıştırma
```bash
dotnet run
```

### Çıktı Örneği
```
=== Fail2Ban Servisi Başlatıldı ===
Versiyon: 1.0.2
Platform: Microsoft Windows NT 10.0.26100.0

=== Ban Sistemleri ===
Log İzleme: Aktif
Event Log İzleme: Aktif
AbuseIPDB Rapor: Aktif
Windows Firewall: Aktif

=== Event Log Filtreleri ===
- EventLog-RDP: MaxFail=3, BanTime=3600s, LogonTypes=[10]
- EventLog-SQLServer: MaxFail=5, BanTime=3600s, Source=MSSQLSERVER

=== Veritabanı İstatistikleri ===
Toplam Ban Sayısı: 15
Aktif Ban Sayısı: 3
Bugünkü Ban Sayısı: 5
```

### Windows Servis Olarak Kurma
```powershell
# Publish edin
dotnet publish --configuration Release --output ./publish

# Windows servis olarak kurun (PowerShell Admin)
sc create "Fail2Ban" binPath="C:\path\to\publish\Fail2Ban.exe"
sc start "Fail2Ban"
```

## 🗄️ Veritabanı Özellikleri

### Ban Kayıtları (BanKaydi Tablosu)
- **IpAdresi**: Engellenen IP adresi
- **YasaklamaZamani**: Ban başlangıç zamanı
- **SilmeZamani**: Ban bitiş zamanı
- **BanSuresiDakika**: Ban süresi (dakika)
- **KuralAdi**: Hangi kural ile banlandı
- **BasarisizGirisSayisi**: Kaç başarısız giriş sonucu banlandı
- **AbuseIPDBRaporTarihi**: AbuseIPDB'ye ne zaman raporlandı
- **Aktif**: Ban hala aktif mi

### Veritabanı İşlemleri
```csharp
// IP banlanmış mı kontrol et
var banli = await databaseService.IpBanliMiAsync("192.168.1.100");

// Aktif ban kayıtlarını al
var aktifBanlar = await databaseService.GetAktifBanKayitlariAsync();

// İstatistikleri al
var stats = await databaseService.GetIstatistiklerAsync();
```

## 📝 Event Log İzleme

### Desteklenen Event ID'ler
- **4625**: An account failed to log on (Security Log)
- **4771**: Kerberos pre-authentication failed (Security Log)  
- **18456**: SQL Server login failed (Application Log)

### RDP Saldırı Tespit Örneği
```
[2025-01-24 14:30:15] warn: RDP Brute Force tespit edildi
IP: 192.168.1.100, Kullanıcı: admin, Logon Type: 10
[2025-01-24 14:30:15] warn: IP adresi engellendi - IP: 192.168.1.100, Süre: 60 dakika
```

### SQL Server Saldırı Tespit Örneği
```
[2025-01-24 14:35:20] warn: SQL Server başarısız giriş - IP: 10.0.0.50, Kullanıcı: sa
[2025-01-24 14:35:20] info: IP adresi AbuseIPDB'ye raporlandı: 10.0.0.50
```

## 🛡️ Güvenlik Özellikleri

### IP Filtering
- Loopback adresleri (127.x.x.x) filtrelenir
- Private IP aralıkları (192.168.x.x, 10.x.x.x, 172.16-31.x.x) filtrelenir
- APIPA adresleri (169.254.x.x) filtrelenir

### AbuseIPDB Duplicate Prevention
- Aynı IP 24 saat içinde tekrar raporlanmaz
- Veritabanında rapor tarihi takip edilir
- API limitlerini aşmayı önler

### Thread Safety
- Tüm database işlemleri için ayrı scope kullanılır
- ConcurrentDictionary ile memory thread-safety
- Background task'ler için isolated database context

## 📊 İstatistikler ve Monitoring

### Veritabanı İstatistikleri
- Toplam ban sayısı
- Aktif ban sayısı  
- Bugünkü ban sayısı
- Bu hafta ban sayısı
- En çok ban yiyen IP'ler (Top 10)
- En çok tetiklenen kurallar (Top 10)

### Log Seviyelerini Ayarlama
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "None",
      "Fail2Ban": "Debug"
    }
  }
}
```

## 🔧 Genişletme

### Yeni Event ID Ekleme
```json
{
  "EventLogSettings": {
    "IzlenenEventIdler": [4625, 4771, 18456, 4776],
    "Filtreler": [
      {
        "Ad": "EventLog-NewEvent",
        "Aktif": true,
        "OzelMaxHata": 3,
        "OzelEngellemeSuresi": 3600,
        "EventLogKaynagi": "Security",
        "Aciklama": "Yeni event türü"
      }
    ]
  }
}
```

### Custom Firewall Manager
```csharp
public class CustomFirewallManager : IFirewallManager
{
    public async Task<bool> BlockIpAsync(string ipAddress)
    {
        // Özel firewall implementasyonu
        return true;
    }
    
    public async Task<bool> UnblockIpAsync(string ipAddress)
    {
        // Özel firewall engel kaldırma
        return true;
    }
}
```

### Custom Database Service
```csharp
public class CustomDatabaseService : IDatabaseService
{
    // Farklı veritabanı (PostgreSQL, MySQL) implementasyonu
}
```

## 🐛 Sorun Giderme

### Yaygın Sorunlar

1. **Event Log erişim hatası**
   ```
   Event Log 'Security' dinlenemedi
   ```
   - Yönetici yetkileri ile çalıştırın
   - Event Log servisi aktif mi kontrol edin

2. **SQLite veritabanı hatası**
   ```
   Database path not found
   ```
   - Yazma izinleri kontrol edin
   - Disk alanı kontrol edin

3. **DbContext threading hatası**
   ```
   A second operation was started on this context instance
   ```
   - Bu sorun artık çözüldü (v1.0.2'de scope kullanılıyor)

4. **AbuseIPDB API hatası**
   ```
   Rate limit exceeded
   ```
   - MinRaporAraligiSaat'i artırın (varsayılan: 24)
   - API key'i kontrol edin

### Debug Modunda Çalıştırma
```json
{
  "Logging": {
    "LogLevel": {
      "Fail2Ban": "Debug"
    }
  }
}
```

### Event Log Test
Windows Event Viewer'da Security ve Application log'larını kontrol edin:
- **Windows + R** → `eventvwr.msc`
- **Windows Logs** → **Security/Application**
- Event ID'leri kontrol edin (4625, 4771, 18456)

## 📁 Proje Yapısı

```
Fail2Ban/
├── Configuration/
│   ├── Fail2BanSettings.cs         # Ana konfigürasyon
│   ├── AbuseIPDBSettings.cs        # AbuseIPDB ayarları
│   ├── EventLogSettings.cs         # Event Log ayarları
│   └── BanSistemleriSettings.cs    # Ban sistemleri kontrolü
├── Data/
│   └── Fail2BanDbContext.cs        # Entity Framework DbContext
├── Models/
│   ├── EngellenenIP.cs             # Memory'deki IP modeli
│   ├── HataliGiris.cs              # Hatalı giriş modeli
│   └── BanKaydi.cs                 # Veritabanı ban kaydı modeli
├── Interfaces/
│   ├── IFail2BanManager.cs         # Ana yönetim arayüzü
│   ├── IFirewallManager.cs         # Firewall yönetim arayüzü
│   ├── IAbuseReporter.cs           # AbuseIPDB raporlama arayüzü
│   ├── IDatabaseService.cs         # Veritabanı işlemleri arayüzü
│   ├── ILogAnalyzer.cs             # Log analiz arayüzü
│   └── IEventLogMonitor.cs         # Event Log izleme arayüzü
├── Services/
│   ├── Fail2BanManager.cs          # Ana yönetim servisi
│   ├── WindowsFirewallManager.cs   # Windows Firewall implementasyonu
│   ├── AbuseIPDBReporter.cs        # AbuseIPDB raporlama servisi
│   ├── DatabaseService.cs          # SQLite veritabanı servisi
│   ├── LogAnalyzer.cs              # Log analiz servisi
│   ├── WindowsEventLogMonitor.cs   # Event Log izleme servisi
│   ├── LogMonitorService.cs        # Background log izleme
│   └── EventLogMonitorService.cs   # Background Event Log izleme
├── Program.cs                      # Ana program ve DI yapılandırması
├── appsettings.json               # Konfigürasyon dosyası
├── fail2ban.db                    # SQLite veritabanı dosyası
└── README.md                      # Bu dokümantasyon
```

## 🎉 Versiyon Geçmişi

### v1.0.2 (2025-01-24)
- ✅ SQLite veritabanı entegrasyonu
- ✅ Windows Event Log izleme
- ✅ AbuseIPDB duplicate kontrolü
- ✅ Thread safety düzeltmeleri
- ✅ Sistem-specific AbuseIPDB mesajları
- ✅ Gelişmiş istatistikler
- ✅ Multiple saldırı türü desteği

### v1.0.1 (2025-01-20)
- ✅ İlk stable release
- ✅ Mail Enable SMTP log desteği
- ✅ Basic AbuseIPDB entegrasyonu
- ✅ Windows Firewall yönetimi

### v1.0.0 (2025-01-15)
- ✅ İlk beta release

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Commit edin (`git commit -am 'Yeni özellik eklendi'`)
4. Push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 📞 Destek

- **Issues**: GitHub Issues
- **Email**: detmach@gmail.com

## 🙏 Teşekkürler

Bu proje geliştirilirken aşağıdaki kaynaklardan yararlanılmıştır:

- [MailEnable ve Mail Sunucu Güvenliği – Windows için Fail2Ban Alternatif](https://cagatayakinci.com/mailenable-ve-mail-sunucu-guvenligi-windows-icin-fail2ban-alternatif/) - Çağatay AKINCI'nın orijinal Fail2Ban Windows implementasyonu

## 🎯 Roadmap

### Yakın Gelecek (v1.1.0)
- [ ] Web interface (dashboard)
- [ ] Email notification sistemi
- [ ] Custom webhook desteği
- [ ] IP whitelist/blacklist yönetimi

### Uzun Vadeli (v2.0.0)
- [ ] Machine learning tabanlı anomali tespiti
- [ ] Distributed/cluster desteği
- [ ] REST API
- [ ] Docker containerization

---

## 🚀 Hızlı Başlangıç

```bash
# 1. Projeyi klonlayın
git clone https://github.com/your-repo/fail2ban-windows.git
cd fail2ban-windows/Fail2Ban

# 2. AbuseIPDB API key'i ekleyin (opsiyonel)
# appsettings.json → AbuseIPDBSettings → ApiKey

# 3. Çalıştırın
dotnet run

# 4. Test için RDP başarısız giriş deneyin (başka bilgisayardan)
# Hemen Event Log'da tespit edilecek ve IP banlanacak!
```

**🎉 Artık Windows sunucunuz otomatik olarak korunuyor!**


