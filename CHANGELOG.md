# Changelog

Fail2Ban Windows projesinin tüm değişiklikleri bu dosyada kaydedilir.

## [1.0.0] - 2025-01-24

### ✨ Eklenen Özellikler
- Fail2Ban Windows alternatifi (.NET 9 Console Application)
- Mail Enable SMTP log analizi
- Windows Firewall otomatik engelleme
- AbuseIPDB entegrasyonu
- Çoklu regex filtre desteği
- JSON konfigürasyon sistemi
- Structured logging (Serilog)
- Background service architecture

### 🔧 Teknik Özellikler
- Dependency Injection pattern
- Interface-based architecture
- Thread-safe operations (ConcurrentDictionary)
- Async/await pattern
- Configuration validation
- Error handling ve logging

### 🚀 GitHub Actions CI/CD
- Otomatik build ve test
- Multi-platform releases (x64, x86, ARM64)
- Security scanning (CodeQL)
- Dependency vulnerability checks
- Automated dependency updates
- Pull request quality checks
- Self-contained ve framework-dependent packages

### 📦 Release Packages
- **Self-Contained**: .NET runtime dahil, bağımsız çalışır
- **Framework-Dependent**: .NET 9.0 runtime gerektirir
- SHA256 checksums ile güvenlik doğrulaması

### 🔒 Güvenlik
- Input validation
- Secure configuration handling
- Automated security audits
- Vulnerability reporting

### 📖 Dokümantasyon
- Comprehensive README
- Installation guide
- Configuration examples
- Usage instructions
- GitHub Actions documentation

## [Unreleased]

### 🔄 Planlanmış Özellikler
- Windows Service desteği
- Web arayüzü
- Email bildirimleri
- Çoklu log dosyası desteği
- Advanced filtering options
- Statistics dashboard

---

## Format Rehberi

Bu changelog [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) formatını takip eder ve [Semantic Versioning](https://semver.org/spec/v2.0.0.html) kullanır.

### Değişiklik Türleri
- `✨ Eklenen`: Yeni özellikler
- `🔧 Değiştirilen`: Mevcut functionality değişiklikleri  
- `🚀 Geliştirildi`: Performance veya UX iyileştirmeleri
- `❌ Kaldırıldı`: Artık desteklenmeyen özellikler
- `🐛 Düzeltildi`: Bug fix'ler
- `🔒 Güvenlik`: Güvenlik ile ilgili değişiklikler 