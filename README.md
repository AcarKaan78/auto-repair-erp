# Auto Repair ERP

A full-featured desktop ERP system built for automotive repair shop management. Handles the complete business workflow from customer intake to service tracking, payment processing, daily expense management, and financial reporting with automated Excel exports.

Built with **.NET 8 WPF**, **SQLite**, and **Material Design**, targeting Windows x64.

---

## Architecture

```
BulentOtoElektrik.sln
├── src/
│   ├── BulentOtoElektrik.Core/            # Domain entities, interfaces, enums, DTOs
│   ├── BulentOtoElektrik.Infrastructure/   # EF Core DbContext, repositories, services
│   ├── BulentOtoElektrik.UI/              # WPF views, view models, converters, resources
│   └── BulentOtoElektrik.App/             # Application entry point, DI configuration
├── tests/
│   └── BulentOtoElektrik.Tests/           # Integration tests (28 tests)
└── installer/
    └── setup.iss                          # Inno Setup installer script
```

**Pattern**: Clean Architecture with MVVM (CommunityToolkit.Mvvm), Repository + Unit of Work, Service Layer.

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | .NET 8 (WPF, Windows Desktop) | 8.0 |
| UI Toolkit | MaterialDesignInXAML | 5.3.0 |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| Database | SQLite via EF Core | 8.x |
| Charts | LiveCharts2 (SkiaSharp) | 2.0.0-rc4.5 |
| Excel Export | ClosedXML | 0.105.0 |
| Logging | Serilog (File Sink) | 4.3.1 |
| Installer | Inno Setup 6 | 6.7.x |

---

## Domain Model

```
Customer (FullName, Phone1, Phone2, IdentityNumber, Email, Address)
  └── Vehicle (PlateNumber, Brand, Model, Year)
        └── ServiceRecord (Date, Complaint, WorkPerformed, Qty, UnitPrice, TotalAmount, Currency)
              └── Technician (FullName, Phone, IsActive)
  └── Payment (Date, Amount, Currency, PaymentMethod)

DailyExpense (Date, Description, Amount, Currency)
  └── ExpenseCategory (Name, IsActive)
```

**Financial Logic**:
- `Customer.Balance = Sum(ServiceRecords.TotalAmount) - Sum(Payments.Amount)`
- `ServiceRecord.TotalAmount = Quantity * UnitPrice` (auto-computed in `SaveChangesAsync`)
- Multi-currency support: TL, USD, EUR
- Payment methods: Cash, Credit Card, Bank Transfer

---

## Features

### Dashboard
Real-time daily overview with revenue/expense cards showing percentage changes from the previous day. Monthly column chart (revenue) overlaid with line chart (expenses). Lists 10 most recent services and top 10 debtors sorted by outstanding balance.

### Customer Management
Full CRUD with dual phone numbers, identity number, email, and address fields. Vehicle tabs display per-vehicle service history. Inline editing with save/cancel. Balance auto-recalculates from service records minus payments.

### Global Search
Debounced search bar (300ms) in the main toolbar. Searches across vehicle plate numbers and customer names using culture-aware `LIKE` queries. Returns top 20 results with autocomplete dropdown showing plate, customer name, vehicle model, and balance.

### Service Entry
Multi-line batch entry form. Search vehicle by plate number with auto-complete. Each line item: complaint, work performed, technician assignment, quantity, unit price, currency. Line totals and grand total computed live. Saves all lines as individual `ServiceRecord` entries in a single transaction.

### Payment Processing
Record payments against a customer or specific vehicle. Supports Cash, Credit Card, and Bank Transfer methods. Updates customer balance in real-time.

### Daily Expense Tracking
Date-navigable expense list with category-based classification. Daily summary card shows revenue, expenses, and net earnings. 12 default expense categories (rent, utilities, parts, payroll, insurance, tax, etc.) with ability to add custom categories.

### Reporting & Analytics
Date range picker with quick filters (today, this week, this month, last month, this year). Generates:
- **Summary totals**: Revenue, expenses, net earnings
- **Daily breakdown table**: Revenue and expenses per day
- **Technician performance**: Revenue and service count per technician
- **Expense breakdown**: Pie chart with category percentages

### Automated Excel Exports
Background export triggered on every data change (fire-and-forget, non-blocking):
- **Customer cards**: Per-vehicle formatted sheet with service history, running balance, color-coded sections
- **Period reports**: 4-sheet workbook (Summary, Services, Payments, Expenses) with navy headers and proper number formatting
- **Auto-generated**: Daily, weekly, monthly, and yearly reports regenerated on each change

### Backup System
- Automatic timestamped SQLite backup on every app startup
- Configurable backup folder via folder picker
- Auto-cleanup retains 30 most recent backups
- Manual backup trigger from Settings page

### Technician Management
Add, edit, and toggle active/inactive status. Technicians are assignable to service records for performance tracking.

### Settings
- Backup folder configuration
- Excel export folder configuration (persisted to `export_settings.txt`)
- Expense category management (add, toggle active)

---

## UI

8 main pages + 5 modal dialogs, navigated via a fixed 220px indigo sidebar.

| Screen | Description |
|--------|-------------|
| Ana Sayfa (Dashboard) | Charts, daily summary, recent services, top debtors |
| Musteri Ara (Search) | Global customer/vehicle search with balance display |
| Customer Detail | Edit customer, manage vehicles, services, payments |
| Yeni Islem (New Service) | Multi-line batch service entry |
| Gunluk Giderler (Expenses) | Date-navigable daily expense tracking |
| Raporlar (Reports) | Date range analytics with charts and Excel export |
| Teknisyenler (Technicians) | Staff management |
| Ayarlar (Settings) | Backup, export, and category configuration |

**Theme**: Material Design Light with Indigo primary (#303F9F) and Amber secondary (#FFC107).

**Localization**: All UI text in Turkish (tr-TR). Dates formatted as `dd.MM.yyyy`, currency as Turkish Lira.

---

## Data Access

### Repository Layer

All repositories use `async/await` with `CancellationToken`, `AsNoTracking()` for reads, and return domain entities.

- **CustomerRepository** - CRUD, search by name/plate (top 50), top debtors by balance
- **VehicleRepository** - CRUD, plate search, returns `VehicleSearchResult` DTOs with computed balance
- **ServiceRecordRepository** - CRUD, batch add, date range queries, today's vehicle count
- **PaymentRepository** - CRUD, customer-scoped queries
- **DailyExpenseRepository** - CRUD, date/range queries
- **TechnicianRepository** - CRUD, active filter
- **ExpenseCategoryRepository** - CRUD, active filter

### Unit of Work

`IUnitOfWork` wraps all repositories with lazy initialization and exposes `SaveChangesAsync()` for transactional commits.

### SQLite Decimal Workaround

SQLite's EF Core provider does not support `Sum()` on `decimal` columns in LINQ-to-SQL. All aggregations materialize with `ToListAsync()` first, then compute sums in LINQ to Objects. Tests use EF Core InMemory provider which handles decimal aggregation natively.

---

## Testing

28 integration tests using xUnit with EF Core InMemory provider.

| Test Class | Coverage |
|-----------|---------|
| CustomerRepositoryTests | CRUD, search, top debtors, balance calculation |
| VehicleRepositoryTests | CRUD, plate search, customer filtering |
| ServiceRecordRepositoryTests | CRUD, batch operations, date range queries |
| PaymentRepositoryTests | CRUD, customer-scoped queries |
| ReportingServiceTests | Daily summary, period reports, technician grouping, expense breakdown |
| ExcelExportServiceTests | File generation, sheet content, formatting |

```bash
dotnet test
```

---

## Build & Run

### Prerequisites

- .NET 8 SDK
- Windows 10/11 (x64)

### Build

```bash
dotnet build BulentOtoElektrik.sln
```

Expected output: 0 errors, ~12 NU1701 warnings (NuGet compatibility — harmless).

### Run

```bash
dotnet run --project src/BulentOtoElektrik.App
```

On first launch:
1. SQLite database auto-created at `{app}/bulentoto.db`
2. Default expense categories and technicians seeded
3. User prompted to select Excel export folder
4. Backup created automatically

### Publish

```bash
dotnet publish src/BulentOtoElektrik.App/BulentOtoElektrik.App.csproj \
  -c Release -r win-x64 --self-contained true -o publish
```

### Build Installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```bash
# One-click build (publish + compile installer)
installer\build-installer.bat
```

Output: `installer/Output/BulentOtoElektrik_Kurulum_v1.0.0.exe` (~58MB, self-contained).

The installer:
- Turkish-language wizard
- Installs to `C:\BulentOtoElektrik` (configurable, avoid Program Files)
- Creates desktop and Start Menu shortcuts
- Registers uninstaller in Add/Remove Programs
- Preserves user data (database, backups, logs) on upgrade
- Asks about data deletion on uninstall

---

## Configuration

| File | Purpose | Location |
|------|---------|----------|
| `appsettings.json` | Backup folder, database path | App directory |
| `export_settings.txt` | Excel export folder path | App directory (auto-created) |
| `bulentoto.db` | SQLite database | App directory (auto-created) |
| `logs/app-*.log` | Daily rolling logs (30-day retention) | App directory |
| `backups/` | Timestamped database backups (30 max) | App directory (configurable) |

---

## Startup Flow

```
1. Set Turkish culture (tr-TR) globally
2. Configure Serilog (daily rolling file logs)
3. Build DI container (DbContext, repositories, services, ViewModels)
4. EnsureCreatedAsync() → create database if not exists
5. DatabaseSeeder → seed default categories and technicians
6. BackupService → create startup backup
7. ExcelExportService → configure export folder (prompt on first launch)
8. AutoExportAllAsync() → regenerate all Excel reports (fire-and-forget)
9. Show MainWindow
```

---

## Project Structure

```
src/BulentOtoElektrik.Core/
├── Entities/          # Customer, Vehicle, ServiceRecord, Payment, DailyExpense, etc.
├── Enums/             # PaymentMethod, CurrencyType
├── Interfaces/        # IRepository, IUnitOfWork, IService contracts
└── DTOs/              # VehicleSearchResult, DailySummaryDto, PeriodReportDto, etc.

src/BulentOtoElektrik.Infrastructure/
├── Data/
│   ├── AppDbContext.cs        # EF Core context with auto-computed TotalAmount
│   └── Seeding/               # DatabaseSeeder
├── Repositories/              # All repository implementations + UnitOfWork
└── Services/                  # ReportingService, ExcelExportService, BackupService

src/BulentOtoElektrik.UI/
├── Views/                     # 8 pages + 5 dialogs (XAML)
├── ViewModels/                # 13 ViewModels (MVVM with CommunityToolkit)
├── Converters/                # 6 value converters (currency, date, visibility, etc.)
├── Helpers/                   # PlateNumberFormatter
└── Resources/                 # Styles, themes, color palette

src/BulentOtoElektrik.App/
├── App.xaml                   # Material Design theme, DataTemplates
├── App.xaml.cs                # Startup, DI configuration, global error handling
└── appsettings.json           # Runtime configuration

tests/BulentOtoElektrik.Tests/
├── Repositories/              # Repository integration tests
├── Services/                  # Service integration tests
└── Helpers/                   # TestDbContextFactory (InMemory provider)

installer/
├── setup.iss                  # Inno Setup script
├── build-installer.bat        # One-click build script
└── app.ico                    # Application icon
```

---

## License

This project is proprietary software.
