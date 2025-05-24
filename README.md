# Fail2Ban Windows Servisi

Windows platformu için geliştirilmiş modern Fail2Ban implementasyonu. Bu uygulama, log dosyalarını izleyerek şüpheli aktiviteleri tespit eder ve Windows Firewall kullanarak otomatik IP engelleme işlemleri gerçekleştirir.

## 🚀 Özellikler

- **Gerçek Zamanlı Log İzleme**: Log dosyalarını sürekli izler ve yeni girişleri analiz eder
- **Esnek Filtre Sistemi**: Regex tabanlı özelleştirilebilir log filtreleri
- **Windows Firewall Entegrasyonu**: Otomatik IP engelleme ve engel kaldırma
- **AbuseIPDB Entegrasyonu**: Şüpheli IP'leri otomatik olarak raporlama
- **Modüler Yapı**: Farklı servisler için kolayca genişletilebilir
- **Kapsamlı Loglama**: Detaylı log kayıtları ve hata takibi
- **Konfigürasyon Tabanlı**: JSON dosyası ile kolay yapılandırma

## 📋 Gereksinimler

- Windows 10/11 veya Windows Server 2016+
- .NET 9.0 Runtime
- Yönetici (Administrator) yetkileri
- Mail Enable SMTP Server (varsayılan konfigürasyon için)

## 🛠️ Kurulum

### 1. Projeyi İndirin veya Klonlayın

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

`appsettings.json` dosyasını ihtiyaçlarınıza göre düzenleyin:

```json
{
  "Fail2BanSettings": {
    "MaxHataliGiris": 3,
    "EngellemeZamani": 18000,
    "LogDosyaYolSablonu": "C:\\Your\\Log\\Path\\LogFile-{0}.log"
  }
}
```

## ⚙️ Konfigürasyon

### Ana Ayarlar (`Fail2BanSettings`)

| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| `MaxHataliGiris` | İzin verilen maksimum hatalı giriş sayısı | 3 |
| `EngellemeZamani` | Engelleme süresi (saniye) | 18000 (5 saat) |
| `KontrolAraligi` | Log kontrol aralığı (milisaniye) | 10000 (10 saniye) |
| `LogDosyaYolSablonu` | Log dosya yolu şablonu | Mail Enable SMTP log yolu |

### Log Filtreleri

Her filtre için aşağıdaki ayarları yapılandırabilirsiniz:

```json
{
  "Ad": "Filtre-Adi",
  "Pattern": "Regex-Pattern",
  "IpGrupAdi": "ipAdresi",
  "Aktif": true,
  "OzelMaxHata": 5,
  "OzelEngellemeSuresi": 3600
}
```

### AbuseIPDB Ayarları

```json
{
  "AbuseIPDBSettings": {
    "ApiKey": "your-api-key",
    "Aktif": true,
    "Kategori": 18
  }
}
```

## 🚦 Kullanım

### Konsol Modunda Çalıştırma

```bash
dotnet run
```

### Windows Servis Olarak Kurma

1. Projeyi `publish` edin:
```bash
dotnet publish --configuration Release --output ./publish
```

2. Windows servis olarak kurun (PowerShell Admin):
```powershell
sc create "Fail2Ban" binPath="C:\path\to\publish\Fail2Ban.exe"
sc start "Fail2Ban"
```

### Manuel IP Engelleme

Kod içerisinden:
```csharp
var fail2BanManager = serviceProvider.GetService<IFail2BanManager>();
await fail2BanManager.BlockIpManuallyAsync("192.168.1.100", 3600, "Manuel Test");
```

## 📝 Log Filtreleri Örnekleri

### SMTP Auth Failed
```regex
^(?<tarih>\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\s+SMTP-IN\s+\w+\.\w+\s+\d+\s+(?<ipAdresi>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+AUTH\s+(?=.*535 Invalid Username or Password)
```

### FTP Brute Force
```regex
^(?<tarih>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\s+.*FTP.*(?<ipAdresi>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}).*(?:login failed|authentication failed)
```

### SSH Brute Force
```regex
^(?<tarih>\w{3}\s+\d{1,2} \d{2}:\d{2}:\d{2}).*sshd.*Failed password for.*from (?<ipAdresi>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})
```

## 🔧 Genişletme

### Yeni Filtre Ekleme

1. `appsettings.json` dosyasına yeni filtre ekleyin
2. Regex pattern'ini test edin
3. Servisi yeniden başlatın

### Farklı Firewall Yöneticisi

`IFirewallManager` interface'ini implement ederek farklı firewall sistemleri destekleyebilirsiniz:

```csharp
public class CustomFirewallManager : IFirewallManager
{
    public async Task<bool> BlockIpAsync(string ipAddress)
    {
        // Özel firewall implementasyonu
    }
}
```

### Farklı Log Kaynaları

`LogMonitorService` sınıfını genişleterek farklı log kaynaklarını destekleyebilirsiniz.

## 🐛 Sorun Giderme

### Yaygın Sorunlar

1. **Log dosyası bulunamıyor**
   - Log dosya yolunu kontrol edin
   - Dosya izinlerini kontrol edin

2. **Firewall kuralları oluşturulamıyor**
   - Yönetici yetkileri ile çalıştırın
   - Windows Firewall servisinin aktif olduğunu kontrol edin

3. **AbuseIPDB raporlaması çalışmıyor**
   - API key'i kontrol edin
   - İnternet bağlantısını kontrol edin

### Log Seviyelerini Ayarlama

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Fail2Ban": "Debug"
    }
  }
}
```

## 📊 Monitoring

Uygulama aşağıdaki metrikleri takip eder:

- Engellenmiş IP sayısı
- Hatalı giriş denemeleri
- İşlenen log satır sayısı
- AbuseIPDB rapor durumları

## 🔒 Güvenlik

- Sadece gerekli IP'leri engelleyin
- Regex pattern'lerini dikkatli test edin
- Log dosyalarına erişimi kısıtlayın
- AbuseIPDB API key'ini güvenli saklayın

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Commit edin (`git commit -am 'Yeni özellik eklendi'`)
4. Push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın.

## 📞 Destek

- Issues: GitHub Issues
- Email: detmach@gmail.com

## 📚 Kaynaklar

Bu proje geliştirilirken aşağıdaki kaynaklardan yararlanılmıştır:

- [MailEnable ve Mail Sunucu Güvenliği – Windows için Fail2Ban Alternatif](https://cagatayakinci.com/mailenable-ve-mail-sunucu-guvenligi-windows-icin-fail2ban-alternatif/) - Çağatay AKINCI tarafından yazılan orijinal Fail2Ban Windows implementasyonu

## 🎉 Fail2Ban Projesi Başarıyla Oluşturuldu!

### 📁 Proje Yapısı

```
Fail2Ban/
├── Configuration/
│   ├── Fail2BanSettings.cs      # Ana konfigürasyon ayarları
│   └── AbuseIPDBSettings.cs     # AbuseIPDB entegrasyon ayarları
├── Models/
│   ├── EngellenenIP.cs          # Engellenmiş IP model
│   └── HataliGiris.cs           # Hatalı giriş model
├── Interfaces/
│   ├── ILogAnalyzer.cs          # Log analiz servisi arayüzü
│   ├── IFirewallManager.cs      # Firewall yönetim arayüzü
│   ├── IAbuseReporter.cs        # Abuse raporlama arayüzü
│   └── IFail2BanManager.cs      # Ana yönetim servisi arayüzü
├── Services/
│   ├── LogAnalyzer.cs           # Log analiz implementasyonu
│   ├── WindowsFirewallManager.cs # Windows Firewall yönetimi
│   ├── AbuseIPDBReporter.cs     # AbuseIPDB raporlama
│   ├── Fail2BanManager.cs       # Ana yönetim servisi
│   └── LogMonitorService.cs     # Background log izleme servisi
├── Program.cs                   # Ana program ve DI yapılandırması
├── appsettings.json            # Konfigürasyon dosyası
└── README.md                   # Dokümantasyon
```

### 🚀 Temel Özellikler

1. **Modüler Yapı**: Her servis ayrı interface ve implementasyon ile ayrılmış
2. **Dependency Injection**: .NET 9 hosting sistemi kullanılarak modern DI yapısı
3. **Asenkron İşlemler**: Tüm I/O işlemleri async/await pattern ile
4. **Thread-Safe**: ConcurrentDictionary kullanarak thread-safe veri yapıları
5. **Kapsamlı Loglama**: Structured logging ile detaylı log kayıtları
6. **Esnek Konfigürasyon**: JSON tabanlı konfigürasyon sistemi

### 🔧 Farklı Bloklamalar İçin Genişletme

Projeyi farklı servisler için genişletmek çok kolay:

#### 1. Yeni Log Filtresi Ekleme
`appsettings.json` dosyasına yeni filtre ekleyin:

```json
{
  "Ad": "FTP-Brute-Force",
  "Pattern": "^(?<tarih>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2})\\s+.*FTP.*(?<ipAdresi>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}).*(?:login failed|authentication failed)",
  "IpGrupAdi": "ipAdresi",
  "Aktif": true,
  "OzelMaxHata": 3,
  "OzelEngellemeSuresi": 7200
}
```

#### 2. Farklı Firewall Sistemi
Yeni bir firewall manager oluşturun:

```csharp
public class PfSenseFirewallManager : IFirewallManager
{
    public async Task<bool> BlockIpAsync(string ipAdresi)
    {
        // pfSense API çağrısı
    }
}
```

#### 3. Farklı Log Kaynağı
`LogMonitorService`'i genişleterek farklı log kaynaklarını destekleyin:

```csharp
public class DatabaseLogMonitorService : BackgroundService
{
    // Veritabanından log okuma
}
```

### 🎯 Kullanım Örnekleri

#### Konsol Modunda Çalıştırma:
```bash
dotnet run
```

#### Manuel IP Engelleme:
```csharp
await fail2BanManager.BlockIpManuallyAsync("192.168.1.100", 3600, "Manuel Test");
```

#### Engellenen IP'leri Listeleme:
```csharp
var blockedIps = fail2BanManager.GetBlockedIps();
```

### 📊 Avantajlar

1. **Temiz Kod**: SOLID prensipleri uygulanmış
2. **Test Edilebilir**: Interface'ler sayesinde unit test yazılabilir
3. **Performanslı**: Regex'ler önceden derlenmiş, thread-safe collections kullanılmış
4. **Güvenilir**: Kapsamlı hata yönetimi ve logging
5. **Esnek**: Konfigürasyon tabanlı, kolayca özelleştirilebilir

### 🔄 Sonraki Adımlar

1. **AbuseIPDB API Key**: `appsettings.json`'da API key'inizi güncelleyin
2. **Log Yolu**: Kendi log dosya yolunuzu ayarlayın
3. **Filtreleri Test Edin**: Regex pattern'lerinizi test edin
4. **Windows Servis**: Production'da Windows servis olarak kurun

# Geliştirme modunda çalıştır
dotnet run

# Release modunda derle
dotnet build --configuration Release

# Publish et
dotnet publish --configuration Release --output ./publish


