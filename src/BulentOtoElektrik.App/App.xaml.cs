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

            // Migrate existing DB: add StockItems table and ServiceRecord columns if missing
            var conn = context.Database.GetDbConnection();
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='StockItems'";
                if (await cmd.ExecuteScalarAsync() == null)
                {
                    using var migrate = conn.CreateCommand();
                    migrate.CommandText = @"
                        CREATE TABLE StockItems (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            MaterialName TEXT NOT NULL,
                            StockQuantity INTEGER NOT NULL,
                            RemainingQuantity INTEGER NOT NULL,
                            UnitPrice REAL NOT NULL DEFAULT 0,
                            IsActive INTEGER NOT NULL DEFAULT 1,
                            CreatedAt TEXT NOT NULL DEFAULT '0001-01-01',
                            UpdatedAt TEXT NOT NULL DEFAULT '0001-01-01'
                        );
                        ALTER TABLE ServiceRecords ADD COLUMN StockItemId INTEGER REFERENCES StockItems(Id);
                        ALTER TABLE ServiceRecords ADD COLUMN MaterialQuantityUsed INTEGER NOT NULL DEFAULT 0;";
                    await migrate.ExecuteNonQueryAsync();
                    Log.Information("StockItems table and ServiceRecord columns created");
                }
            }

            // Migrate: add Personnel table if missing
            using (var cmd2 = conn.CreateCommand())
            {
                cmd2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Personnel'";
                if (await cmd2.ExecuteScalarAsync() == null)
                {
                    using var migrate2 = conn.CreateCommand();
                    migrate2.CommandText = @"
                        CREATE TABLE Personnel (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FullName TEXT NOT NULL,
                            TcKimlikNo TEXT,
                            Phone TEXT,
                            Role TEXT,
                            IsActive INTEGER NOT NULL DEFAULT 1,
                            CreatedAt TEXT NOT NULL DEFAULT '0001-01-01',
                            UpdatedAt TEXT NOT NULL DEFAULT '0001-01-01'
                        );";
                    await migrate2.ExecuteNonQueryAsync();
                    Log.Information("Personnel table created");
                }
            }

            // Migrate: add TcKimlikNo column to Personnel if missing
            using (var cmd3 = conn.CreateCommand())
            {
                cmd3.CommandText = "PRAGMA table_info(Personnel)";
                bool hasTcColumn = false;
                using var reader = await cmd3.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader.GetString(1) == "TcKimlikNo")
                    {
                        hasTcColumn = true;
                        break;
                    }
                }
                if (!hasTcColumn)
                {
                    using var migrate3 = conn.CreateCommand();
                    migrate3.CommandText = "ALTER TABLE Personnel ADD COLUMN TcKimlikNo TEXT";
                    await migrate3.ExecuteNonQueryAsync();
                    Log.Information("TcKimlikNo column added to Personnel table");
                }
            }

            // Migrate: convert all USD/EURO currency values to TL
            using (var cmd4 = conn.CreateCommand())
            {
                cmd4.CommandText = @"
                    UPDATE ServiceRecords SET Currency = 'TL' WHERE Currency IN ('USD', 'EURO');
                    UPDATE Payments SET Currency = 'TL' WHERE Currency IN ('USD', 'EURO');
                    UPDATE DailyExpenses SET Currency = 'TL' WHERE Currency IN ('USD', 'EURO');";
                var affected = await cmd4.ExecuteNonQueryAsync();
                if (affected > 0)
                    Log.Information("Migrated {Count} records from USD/EURO to TL", affected);
            }

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

        // Load saved Excel export folder if configured
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
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load export folder setting");
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
        services.AddScoped<IStockItemRepository, StockItemRepository>();
        services.AddScoped<IPersonnelRepository, PersonnelRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddSingleton<IExcelExportService>(sp => new ExcelExportService(sp));
        services.AddSingleton<IExcelImportService>(sp => new ExcelImportService(sp));
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
