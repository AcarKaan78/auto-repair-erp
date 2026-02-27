using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure;
using BulentOtoElektrik.Infrastructure.Data;
using BulentOtoElektrik.Infrastructure.Data.Seeding;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BulentOtoElektrik.App;

public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;
    public static IServiceProvider ServiceProvider => _serviceProvider!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set Turkish culture globally
        var culture = new CultureInfo("tr-TR");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        // Configure DI
        _serviceProvider = ConfigureServices();

        // Global exception handler
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception");
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu. Lütfen uygulamayı yeniden başlatın.\n\n" +
                $"Hata: {args.Exception.Message}",
                "Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        // Initialize database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();

            Log.Information("Database initialized and seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize database");
            MessageBox.Show(
                $"Veritabanı başlatılamadı: {ex.Message}",
                "Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // Auto-backup on startup
        try
        {
            var backupService = _serviceProvider.GetRequiredService<IBackupService>();
            await backupService.CreateBackupAsync();
            Log.Information("Startup backup created successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create startup backup");
        }

        // First-launch: ask user for Excel export folder if not set
        try
        {
            var excelService = _serviceProvider.GetRequiredService<IExcelExportService>();
            var settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "export_settings.txt");
            if (File.Exists(settingsFile))
            {
                var savedFolder = File.ReadAllText(settingsFile).Trim();
                if (!string.IsNullOrEmpty(savedFolder))
                    excelService.SetExportFolder(savedFolder);
            }
            else
            {
                MessageBox.Show(
                    "Excel dosyalarını çıkartmak istediğiniz yeri seçin.",
                    "Klasör Seçimi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Excel dosyaları için kayıt klasörünü seçin",
                    Multiselect = false
                };
                if (dialog.ShowDialog() == true)
                {
                    excelService.SetExportFolder(dialog.FolderName);
                    File.WriteAllText(settingsFile, dialog.FolderName);
                }
                else
                {
                    // Use default folder
                    File.WriteAllText(settingsFile, excelService.GetExportFolder());
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set export folder");
        }

        // Auto-export all Excel files on startup (fire-and-forget)
        try
        {
            var excelExportService = _serviceProvider.GetRequiredService<IExcelExportService>();
            _ = excelExportService.AutoExportAllAsync();
            Log.Information("Startup Excel auto-export initiated");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initiate startup Excel export");
        }

        // Show main window (from UI project)
        var mainWindow = _serviceProvider.GetRequiredService<BulentOtoElektrik.UI.Views.MainWindow>();
        mainWindow.Show();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bulentoto.db");

        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Seeder
        services.AddTransient<DatabaseSeeder>();

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceRecordRepository, ServiceRecordRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDailyExpenseRepository, DailyExpenseRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddSingleton<IExcelExportService>(sp => new ExcelExportService(sp));
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IReportingService>(sp => new ReportingService(sp));

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        // UI Services and ViewModels will be registered by the UI project
        BulentOtoElektrik.UI.ServiceRegistration.RegisterServices(services);

        return services.BuildServiceProvider();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
