using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Fail2Ban.Configuration;
using Fail2Ban.Interfaces;
using Fail2Ban.Services;
using Fail2Ban.Data;

namespace Fail2Ban;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Fail2Ban Windows Servis ===");
        Console.WriteLine("Başlatılıyor...");

        try
        {
            var host = CreateHostBuilder(args).Build();
            
            // Veritabanını initialize et
            await InitializeDatabaseAsync(host);
            
            // Uygulama başlangıç bilgilerini göster
            await ShowStartupInfoAsync(host);
            
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uygulama başlatılırken kritik hata oluştu: {ex.Message}");
            Console.WriteLine("Detaylar:");
            Console.WriteLine(ex.ToString());
            
            Console.WriteLine("\nÇıkmak için herhangi bir tuşa basın...");
            Console.ReadKey();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                    optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Konfigürasyon ayarları
                services.Configure<Fail2BanSettings>(
                    context.Configuration.GetSection("Fail2BanSettings"));
                services.Configure<AbuseIPDBSettings>(
                    context.Configuration.GetSection("AbuseIPDBSettings"));

                // SQLite veritabanı
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=fail2ban.db";
                services.AddDbContext<Fail2BanDbContext>(options =>
                    options.UseSqlite(connectionString));

                // HTTP Client
                services.AddHttpClient<IAbuseReporter, AbuseIPDBReporter>();

                // Servisler
                services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
                services.AddSingleton<IFirewallManager, WindowsFirewallManager>();
                services.AddSingleton<IAbuseReporter, AbuseIPDBReporter>();
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddSingleton<IFail2BanManager, Fail2BanManager>();

                // Background servis
                services.AddHostedService<LogMonitorService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                
                // Windows Event Log desteği ekle (opsiyonel)
                if (OperatingSystem.IsWindows())
                {
                    logging.AddEventLog();
                }
            })
            .UseConsoleLifetime();

    /// <summary>
    /// Veritabanını initialize eder ve mevcut ban kayıtlarını yükler
    /// </summary>
    static async Task InitializeDatabaseAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Veritabanı başlatılıyor...");
            
            // Veritabanını oluştur
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            await databaseService.InitializeDatabaseAsync();
            
            // Fail2Ban manager'ı veritabanından initialize et
            var fail2BanManager = scope.ServiceProvider.GetRequiredService<IFail2BanManager>();
            if (fail2BanManager is Fail2BanManager manager)
            {
                await manager.InitializeFromDatabaseAsync();
            }
            
            logger.LogInformation("Veritabanı başarıyla initialize edildi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Veritabanı initialize edilirken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Uygulama başlangıç bilgilerini gösterir
    /// </summary>
    static async Task ShowStartupInfoAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var fail2BanSettings = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Fail2BanSettings>>().Value;
        var abuseSettings = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AbuseIPDBSettings>>().Value;

        logger.LogInformation("=== Fail2Ban Servisi Başlatıldı ===");
        logger.LogInformation("Versiyon: 1.0.1");
        logger.LogInformation("Platform: {Platform}", Environment.OSVersion.VersionString);
        logger.LogInformation("Framework: {Framework}", Environment.Version);
        
        logger.LogInformation("=== Konfigürasyon ===");
        logger.LogInformation("Maksimum Hatalı Giriş: {MaxFail}", fail2BanSettings.MaxHataliGiris);
        logger.LogInformation("Engelleme Süresi: {BanTime} saniye ({Minutes} dakika)", 
            fail2BanSettings.EngellemeZamani, fail2BanSettings.EngellemeZamani / 60);
        logger.LogInformation("Kontrol Aralığı: {Interval} ms", fail2BanSettings.KontrolAraligi);
        logger.LogInformation("Log Dosya Şablonu: {Template}", fail2BanSettings.LogDosyaYolSablonu);
        
        logger.LogInformation("=== Aktif Filtreler ===");
        var activeFilters = fail2BanSettings.LogFiltreler.Where(f => f.Aktif).ToList();
        if (activeFilters.Any())
        {
            foreach (var filter in activeFilters)
            {
                var maxFail = filter.OzelMaxHata ?? fail2BanSettings.MaxHataliGiris;
                var banTime = filter.OzelEngellemeSuresi ?? fail2BanSettings.EngellemeZamani;
                
                logger.LogInformation("- {FilterName}: MaxFail={MaxFail}, BanTime={BanTime}s", 
                    filter.Ad, maxFail, banTime);
            }
        }
        else
        {
            logger.LogWarning("Aktif filtre bulunamadı!");
        }

        logger.LogInformation("=== AbuseIPDB ===");
        logger.LogInformation("AbuseIPDB Aktif: {Active}", abuseSettings.Aktif);
        if (abuseSettings.Aktif)
        {
            var hasApiKey = !string.IsNullOrWhiteSpace(abuseSettings.ApiKey);
            logger.LogInformation("API Key Tanımlı: {HasKey}", hasApiKey);
            if (!hasApiKey)
            {
                logger.LogWarning("AbuseIPDB aktif ancak API key tanımlı değil!");
            }
        }

        // Veritabanı istatistiklerini göster
        try
        {
            using var scope = host.Services.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
            var stats = await databaseService.GetIstatistiklerAsync();
            
            logger.LogInformation("=== Veritabanı İstatistikleri ===");
            logger.LogInformation("Toplam Ban Sayısı: {Total}", stats.GetValueOrDefault("ToplamBan", 0));
            logger.LogInformation("Aktif Ban Sayısı: {Active}", stats.GetValueOrDefault("AktifBan", 0));
            logger.LogInformation("Bugünkü Ban Sayısı: {Today}", stats.GetValueOrDefault("BugunBan", 0));
            logger.LogInformation("Bu Hafta Ban Sayısı: {Week}", stats.GetValueOrDefault("BuHaftaBan", 0));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Veritabanı istatistikleri alınamadı");
        }

        // Güncel log dosyasını kontrol et
        var today = DateTime.Today;
        var formattedDate = $"{today.Year % 100:D2}{today.Month:D2}{today.Day:D2}";
        var currentLogFile = string.Format(fail2BanSettings.LogDosyaYolSablonu, formattedDate);
        
        logger.LogInformation("=== Log Dosyası ===");
        logger.LogInformation("Güncel Log Dosyası: {FilePath}", currentLogFile);
        
        if (File.Exists(currentLogFile))
        {
            var fileInfo = new FileInfo(currentLogFile);
            logger.LogInformation("Dosya Boyutu: {Size:N0} bytes", fileInfo.Length);
            logger.LogInformation("Son Değişiklik: {LastWrite}", fileInfo.LastWriteTime);
        }
        else
        {
            logger.LogWarning("Log dosyası bulunamadı! Dosya oluşturulana kadar bekleniyor...");
        }

        // Mevcut firewall kurallarını kontrol et
        try
        {
            var firewallManager = host.Services.GetRequiredService<IFirewallManager>();
            var existingBlocks = await firewallManager.GetBlockedIpsAsync();
            
            logger.LogInformation("=== Mevcut Firewall Kuralları ===");
            logger.LogInformation("Mevcut Fail2Ban Kuralı Sayısı: {Count}", existingBlocks.Count);
            
            if (existingBlocks.Any())
            {
                logger.LogInformation("Engellenmiş IP'ler: {IPs}", string.Join(", ", existingBlocks.Take(5)));
                if (existingBlocks.Count > 5)
                {
                    logger.LogInformation("... ve {More} tane daha", existingBlocks.Count - 5);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Mevcut firewall kuralları kontrol edilemedi");
        }

        logger.LogInformation("=== Servis Hazır ===");
        logger.LogInformation("Log izleme başlıyor...");
        logger.LogInformation("Durdurmak için Ctrl+C tuşlarına basın");
        
        Console.WriteLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Fail2Ban servisi aktif!");
        Console.WriteLine("Gerçek zamanlı log izleme başlatıldı...");
        Console.WriteLine("SQLite veritabanı ile kalıcı ban kayıtları aktif!");
        Console.WriteLine("\nDurdurmak için Ctrl+C kombinasyonunu kullanın.");
    }
} 