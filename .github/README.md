# GitHub Actions Workflows

Bu dizin, Fail2Ban projesi için GitHub Actions workflow'larını içerir.

## 📋 Workflow'lar

### 1. Pull Request Check (`pr-check.yml`)
**Tetikleyiciler:**
- Pull Request açıldığında, güncellendiğinde

**İşlevler:**
- ✅ Kod formatı kontrolü
- 🏗️ Build doğrulaması (Ubuntu ve Windows)
- 🔍 Dependency güvenlik kontrolü
- 📝 Otomatik PR yorumu
- ⚠️ TODO/FIXME kontrolü
- 📏 Dosya boyutu kontrolü
- 🧪 JSON konfigürasyon doğrulaması

### 2. Dependency Update (`dependency-update.yml`)
**Tetikleyiciler:**
- Her Pazartesi 09:00 UTC (zamanlanmış)
- Manuel tetikleme

**İşlevler:**
- 🔄 NuGet paket güncellemeleri
- 🛡️ Güvenlik açığı taraması
- 📋 Otomatik PR oluşturma
- 🚨 Güvenlik raporu gösterimi

### 3. Release (`release.yml`)
**Tetikleyiciler:**
- Git tag push (`v*.*.*` formatında)
- Manuel tetikleme

**İşlevler:**
- 🏗️ Multi-platform build (x64, x86)
- 📦 Self-contained ve framework-dependent paketler
- 🔐 SHA256 checksum oluşturma
- 📝 Otomatik release notes
- 🚀 GitHub Release oluşturma

## 🔧 Konfigürasyon

### Environment Variables
```yaml
DOTNET_VERSION: '9.0.x'    # .NET SDK versiyonu
PROJECT_PATH: '.'          # Proje dizini (root)
```

### Secrets (Gerekli)
- `GITHUB_TOKEN`: Otomatik olarak sağlanır
- Ek secret'lar gerekli değil

## 📊 Build Matrix

### Desteklenen Platformlar
| Platform | Runtime ID | Açıklama |
|----------|------------|----------|
| Windows x64 | `win-x64` | 64-bit Windows |
| Windows x86 | `win-x86` | 32-bit Windows |

### Paket Türleri
1. **Self-Contained**: .NET runtime dahil, bağımsız çalışır
2. **Framework-Dependent**: .NET 9.0 runtime gerektirir

## 🚀 Kullanım

### Release Oluşturma
```bash
# Git tag ile otomatik release
git tag v1.0.0
git push origin v1.0.0

# Manuel release (GitHub Actions sekmesinden)
# Workflow: Release -> Run workflow
```

### Dependency Güncellemeleri
```bash
# Manuel tetikleme (GitHub Actions sekmesinden)
# Workflow: Dependency Update -> Run workflow
```

## 📈 Workflow Durumu

[![Pull Request Check](../../actions/workflows/pr-check.yml/badge.svg)](../../actions/workflows/pr-check.yml)
[![Dependency Update](../../actions/workflows/dependency-update.yml/badge.svg)](../../actions/workflows/dependency-update.yml)
[![Release](../../actions/workflows/release.yml/badge.svg)](../../actions/workflows/release.yml)

## 🔍 Troubleshooting

### Build Hataları
- .NET SDK versiyonunu kontrol edin
- NuGet paket uyumluluğunu doğrulayın
- Windows Firewall API'si gereksinimlerini kontrol edin

### Release Sorunları
- Tag formatının `v*.*.*` olduğundan emin olun
- GitHub token izinlerini kontrol edin
- Artifact boyut limitlerini kontrol edin

### Dependency Sorunları
- Vulnerable paketleri manuel olarak güncelleyin
- Breaking change'ler için major version güncellemelerini kontrol edin

## 📝 Notlar

- Tüm workflow'lar Windows uyumluluğu için optimize edilmiştir
- Basit ve güvenilir pipeline'lar tercih edilmiştir
- Release artifact'ları 90 gün saklanır
- Proje dosyaları repository root dizininde bulunur
- Token izinleri problemi olan özellikler kaldırılmıştır