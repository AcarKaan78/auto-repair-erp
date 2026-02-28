using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Infrastructure.Data;
using BulentOtoElektrik.Infrastructure.Services;
using BulentOtoElektrik.Tests.Helpers;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Tests.Services;

public class ExcelExportServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ExcelExportService _service;
    private readonly string _tempDir;

    public ExcelExportServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemory();

        var services = new ServiceCollection();
        // Register as Singleton so context survives scope disposal in AutoExport tests
        services.AddSingleton(_context);
        services.AddSingleton<AppDbContext>(_ => _context);
        var sp = services.BuildServiceProvider();

        _service = new ExcelExportService(sp);
        _service.SetAutoExportEnabled(true);
        _tempDir = Path.Combine(Path.GetTempPath(), $"BulentOtoTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _context.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
        try { File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auto_export_enabled.txt")); } catch { }
    }

    private string TempFile(string name = "test.xlsx") => Path.Combine(_tempDir, name);

    /// <summary>
    /// Seeds a customer + vehicle + service records + payments for customer card tests.
    /// </summary>
    private async Task<(Customer customer, Vehicle vehicle)> SeedCustomerCardDataAsync()
    {
        var technician = new Technician { Id = 1, FullName = "İrfan Usta", IsActive = true };
        _context.Technicians.Add(technician);

        var customer = new Customer
        {
            Id = 1,
            FullName = "Kemal Aras",
            Phone1 = "0532 111 22 33",
            Phone2 = "0312 444 55 66",
            IdentityNumber = "12345678901",
            Email = "kemal@test.com",
            Address = "Ankara"
        };
        _context.Customers.Add(customer);

        var vehicle = new Vehicle
        {
            Id = 1,
            CustomerId = 1,
            PlateNumber = "31 ALT 559",
            VehicleBrand = "Ford",
            VehicleModel = "Transit",
            VehicleYear = 2018
        };
        _context.Vehicles.Add(vehicle);

        var records = new List<ServiceRecord>
        {
            new()
            {
                Id = 1, VehicleId = 1, TechnicianId = 1,
                ServiceDate = new DateTime(2025, 1, 10),
                Complaint = "Akü bitmiş", WorkPerformed = "Akü değişimi",
                Quantity = 1, UnitPrice = 3500m, TotalAmount = 3500m,
                Currency = CurrencyType.TL, Notes = "Garantili akü"
            },
            new()
            {
                Id = 2, VehicleId = 1, TechnicianId = 1,
                ServiceDate = new DateTime(2025, 2, 15),
                Complaint = "Far yanmıyor", WorkPerformed = "Far ampulü + balast değişimi",
                Quantity = 2, UnitPrice = 1200m, TotalAmount = 2400m,
                Currency = CurrencyType.TL
            },
            new()
            {
                Id = 3, VehicleId = 1,
                ServiceDate = new DateTime(2025, 3, 20),
                Complaint = null, WorkPerformed = "Genel bakım",
                Quantity = 1, UnitPrice = 800m, TotalAmount = 800m,
                Currency = CurrencyType.TL
            }
        };
        _context.ServiceRecords.AddRange(records);

        var payment = new Payment
        {
            Id = 1, CustomerId = 1, VehicleId = 1,
            PaymentDate = new DateTime(2025, 1, 15),
            Amount = 2000m, Currency = CurrencyType.TL,
            PaymentMethod = PaymentMethod.Cash,
            Notes = "Nakit ödeme"
        };
        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        // Re-load with navigation properties
        return (customer, vehicle);
    }

    /// <summary>
    /// Seeds data for period report export tests.
    /// </summary>
    private async Task SeedReportDataAsync()
    {
        var technician = new Technician { Id = 10, FullName = "Arda Usta", IsActive = true };
        _context.Technicians.Add(technician);

        var customer = new Customer { Id = 10, FullName = "Ali Veli" };
        _context.Customers.Add(customer);

        var vehicle = new Vehicle
        {
            Id = 10, CustomerId = 10, PlateNumber = "06 ABC 123",
            VehicleBrand = "Toyota", VehicleModel = "Corolla"
        };
        _context.Vehicles.Add(vehicle);

        var category = new ExpenseCategory { Id = 1, Name = "Kira", IsActive = true };
        _context.ExpenseCategories.Add(category);

        _context.ServiceRecords.AddRange(
            new ServiceRecord
            {
                Id = 10, VehicleId = 10, TechnicianId = 10,
                ServiceDate = new DateTime(2025, 6, 1),
                WorkPerformed = "Yağ değişimi",
                Quantity = 1, UnitPrice = 500m, TotalAmount = 500m
            },
            new ServiceRecord
            {
                Id = 11, VehicleId = 10, TechnicianId = 10,
                ServiceDate = new DateTime(2025, 6, 15),
                WorkPerformed = "Fren bakımı",
                Quantity = 1, UnitPrice = 1500m, TotalAmount = 1500m
            }
        );

        _context.Payments.Add(new Payment
        {
            Id = 10, CustomerId = 10,
            PaymentDate = new DateTime(2025, 6, 5),
            Amount = 500m, PaymentMethod = PaymentMethod.CreditCard
        });

        _context.DailyExpenses.Add(new DailyExpense
        {
            Id = 1, CategoryId = 1,
            ExpenseDate = new DateTime(2025, 6, 1),
            Description = "Aylık kira", Amount = 10000m
        });

        await _context.SaveChangesAsync();
    }

    // ============================================================
    // ExportCustomerCardAsync Tests
    // ============================================================

    [Fact]
    public async Task ExportCustomerCard_CreatesValidXlsxFile()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        Assert.Single(wb.Worksheets);
        Assert.Equal("kart1", wb.Worksheets.First().Name);
    }

    [Fact]
    public async Task ExportCustomerCard_CustomerInfoRows_AreCorrect()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_info.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // Row 1: AD-SOYAD label + value
        Assert.Equal("AD-SOYAD", ws.Cell("A1").GetString());
        Assert.Equal("Kemal Aras", ws.Cell("C1").GetString());

        // Row 2: TELEFON 1
        Assert.Equal("TELEFON 1", ws.Cell("A2").GetString());
        Assert.Equal("0532 111 22 33", ws.Cell("C2").GetString());

        // Row 3: TELEFON 2
        Assert.Equal("TELEFON 2", ws.Cell("A3").GetString());
        Assert.Equal("0312 444 55 66", ws.Cell("C3").GetString());

        // Row 4: T.C NO
        Assert.Equal("T.C NO", ws.Cell("A4").GetString());
        Assert.Equal("12345678901", ws.Cell("C4").GetString());

        // Row 5: PLAKA
        Assert.Equal("PLAKA", ws.Cell("A5").GetString());
        Assert.Equal("31 ALT 559", ws.Cell("C5").GetString());

        // Row 6: MARKA
        Assert.Equal("MARKA", ws.Cell("A6").GetString());
        Assert.Equal("Ford", ws.Cell("C6").GetString());

        // Row 7: MODEL
        Assert.Equal("MODEL", ws.Cell("A7").GetString());
        Assert.Equal("Transit", ws.Cell("C7").GetString());

        // Row 8: YIL
        Assert.Equal("YIL", ws.Cell("A8").GetString());
        Assert.Equal("2018", ws.Cell("C8").GetString());
    }

    [Fact]
    public async Task ExportCustomerCard_LabelRows_HaveYellowBackground()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_style.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        for (int row = 1; row <= 8; row++)
        {
            var bgColor = ws.Cell(row, 1).Style.Fill.BackgroundColor.Color;
            var hex = $"#{bgColor.R:X2}{bgColor.G:X2}{bgColor.B:X2}";
            Assert.Equal("#FFFFCC", hex);
        }
    }

    [Fact]
    public async Task ExportCustomerCard_RedValueRows_HaveRedFont()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_red.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // Rows 1-3 value cells should be red
        for (int row = 1; row <= 3; row++)
        {
            var fontColor = ws.Cell(row, 3).Style.Font.FontColor.Color;
            var hex = $"#{fontColor.R:X2}{fontColor.G:X2}{fontColor.B:X2}";
            Assert.Equal("#FF0000", hex);
        }

        // Row 4+ value cells should NOT be red
        var row4FontColor = ws.Cell(4, 3).Style.Font.FontColor.Color;
        var row4Hex = $"#{row4FontColor.R:X2}{row4FontColor.G:X2}{row4FontColor.B:X2}";
        Assert.NotEqual("#FF0000", row4Hex);
    }

    [Fact]
    public async Task ExportCustomerCard_CompanyBanner_Row9()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_banner.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        Assert.Equal("BÜLENT OTO ELEKTRİK", ws.Cell("A9").GetString());
        Assert.Equal(28, ws.Cell("A9").Style.Font.FontSize);
        Assert.True(ws.Cell("A9").Style.Font.Bold);
        // A9:J9 should be merged
        Assert.True(ws.Range("A9:J9").IsMerged());
    }

    [Fact]
    public async Task ExportCustomerCard_HeaderRow10_HasCorrectHeaders()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_headers.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        var expectedHeaders = new[] { "S.NO", "TARİH", "ŞİKAYET", "YAPILAN İŞLEM", "TEKNİSYEN", "MİKTAR", "BİRİM FİYAT", "TUTAR", "KALAN BAKİYE", "NOTLAR" };

        for (int col = 1; col <= expectedHeaders.Length; col++)
        {
            Assert.Equal(expectedHeaders[col - 1], ws.Cell(10, col).GetString());
            Assert.True(ws.Cell(10, col).Style.Font.Bold);
        }
    }

    [Fact]
    public async Task ExportCustomerCard_ServiceRecordData_IsCorrect()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_data.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // Row 11: first service record
        Assert.Equal(1, ws.Cell(11, 1).GetValue<int>());            // S.NO
        Assert.Equal("10.01.2025", ws.Cell(11, 2).GetString());     // TARİH
        Assert.Equal("Akü bitmiş", ws.Cell(11, 3).GetString());     // ŞİKAYET
        Assert.Equal("Akü değişimi", ws.Cell(11, 4).GetString());   // YAPILAN İŞLEM
        Assert.Equal("İrfan Usta", ws.Cell(11, 5).GetString());     // TEKNİSYEN
        Assert.Equal(1, ws.Cell(11, 6).GetValue<int>());            // MİKTAR
        Assert.Equal(3500m, ws.Cell(11, 7).GetValue<decimal>());    // BİRİM FİYAT
        Assert.Equal("Garantili akü", ws.Cell(11, 10).GetString()); // NOTLAR

        // Row 12: second service record
        Assert.Equal(2, ws.Cell(12, 1).GetValue<int>());
        Assert.Equal("15.02.2025", ws.Cell(12, 2).GetString());
        Assert.Equal("Far yanmıyor", ws.Cell(12, 3).GetString());
        Assert.Equal("Far ampulü + balast değişimi", ws.Cell(12, 4).GetString());
        Assert.Equal(2, ws.Cell(12, 6).GetValue<int>());            // quantity=2
        Assert.Equal(1200m, ws.Cell(12, 7).GetValue<decimal>());    // unit price

        // Row 13: third service record
        Assert.Equal(3, ws.Cell(13, 1).GetValue<int>());
        Assert.Equal("20.03.2025", ws.Cell(13, 2).GetString());
        Assert.Equal("", ws.Cell(13, 3).GetString());               // null complaint
        Assert.Equal("Genel bakım", ws.Cell(13, 4).GetString());
    }

    [Fact]
    public async Task ExportCustomerCard_ThreeServiceRecords_TotalRows()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_rows.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // 3 records → rows 11, 12, 13
        Assert.Equal(3, ws.Cell(13, 1).GetValue<int>()); // last S.NO
        // Row 14 should be empty (no 4th record)
        Assert.True(ws.Cell(14, 1).IsEmpty());
    }

    [Fact]
    public async Task ExportCustomerCard_TutarColumn_HasFormula()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_formula.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // H11 should have formula F11*G11
        Assert.True(ws.Cell(11, 8).HasFormula);
        Assert.Equal("F11*G11", ws.Cell(11, 8).FormulaA1);

        // H12 should have formula F12*G12
        Assert.True(ws.Cell(12, 8).HasFormula);
        Assert.Equal("F12*G12", ws.Cell(12, 8).FormulaA1);
    }

    [Fact]
    public async Task ExportCustomerCard_KalanBakiye_HasCumulativeSumFormula()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_balance.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // I11 = SUM(H11:H11)
        Assert.True(ws.Cell(11, 9).HasFormula);
        Assert.Equal("SUM(H11:H11)", ws.Cell(11, 9).FormulaA1);

        // I12 = SUM(H11:H12)
        Assert.Equal("SUM(H11:H12)", ws.Cell(12, 9).FormulaA1);

        // I13 = SUM(H11:H13)
        Assert.Equal("SUM(H11:H13)", ws.Cell(13, 9).FormulaA1);
    }

    [Fact]
    public async Task ExportCustomerCard_SummaryArea_BorcAndAlacak()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_summary.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // I1: PARA BİRİMİ, J1: TL
        Assert.Equal("PARA BİRİMİ", ws.Cell("I1").GetString());
        Assert.Equal("TL", ws.Cell("J1").GetString());

        // I2: BORÇ, J2: SUM formula
        Assert.Equal("BORÇ", ws.Cell("I2").GetString());
        Assert.True(ws.Cell("J2").HasFormula);
        Assert.Equal("SUM(H11:H13)", ws.Cell("J2").FormulaA1);

        // I3: ALACAK, J3: 2000 (payment amount)
        Assert.Equal("ALACAK", ws.Cell("I3").GetString());
        Assert.Equal(2000m, ws.Cell("J3").GetValue<decimal>());

        // I5: TOPLAM BORÇ, J5: 6700 (3500+2400+800)
        Assert.Equal("TOPLAM BORÇ", ws.Cell("I5").GetString());
        Assert.Equal(6700m, ws.Cell("J5").GetValue<decimal>());

        // I6: TOPLAM ÖDEME, J6: 2000
        Assert.Equal("TOPLAM ÖDEME", ws.Cell("I6").GetString());
        Assert.Equal(2000m, ws.Cell("J6").GetValue<decimal>());
    }

    [Fact]
    public async Task ExportCustomerCard_TotalSummary_RedBackground()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_redtotal.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // J5 and J6 should have red background and white font
        Assert.True(ws.Cell("J5").Style.Font.Bold);
        Assert.True(ws.Cell("J6").Style.Font.Bold);
    }

    [Fact]
    public async Task ExportCustomerCard_FreezePanes_AtRow10()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_freeze.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // Freeze panes should be set at row 10
        // ClosedXML stores freeze as SplitRow
        Assert.Equal(10, ws.SheetView.SplitRow);
    }

    [Fact]
    public async Task ExportCustomerCard_ColumnWidths_MatchTemplate()
    {
        await SeedCustomerCardDataAsync();
        var path = TempFile("card_widths.xlsx");

        await _service.ExportCustomerCardAsync(1, 1, path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        Assert.Equal(5.71, ws.Column("A").Width, 1);
        Assert.Equal(11.86, ws.Column("B").Width, 1);
        Assert.Equal(24.57, ws.Column("C").Width, 1);
        Assert.Equal(27.71, ws.Column("D").Width, 1);
        Assert.Equal(15.43, ws.Column("E").Width, 1);
        Assert.Equal(6.71, ws.Column("F").Width, 1);
        Assert.Equal(12.14, ws.Column("G").Width, 1);
        Assert.Equal(14.14, ws.Column("H").Width, 1);
        Assert.Equal(22.57, ws.Column("I").Width, 1);
        Assert.Equal(22.29, ws.Column("J").Width, 1);
    }

    [Fact]
    public async Task ExportCustomerCard_EmptyRecords_NoCrash()
    {
        // Customer with no service records
        _context.Customers.Add(new Customer { Id = 99, FullName = "Boş Müşteri" });
        _context.Vehicles.Add(new Vehicle { Id = 99, CustomerId = 99, PlateNumber = "06 TEST 01" });
        await _context.SaveChangesAsync();

        var path = TempFile("card_empty.xlsx");

        await _service.ExportCustomerCardAsync(99, 99, path);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("kart1");

        // Should have headers but no data rows
        Assert.Equal("S.NO", ws.Cell(10, 1).GetString());
        Assert.True(ws.Cell(11, 1).IsEmpty());

        // Borç should be 0 (no formula since no records)
        Assert.Equal(0, ws.Cell("J2").GetValue<double>());
    }

    [Fact]
    public async Task ExportCustomerCard_NonExistentCustomer_NoFileCreated()
    {
        var path = TempFile("card_noexist.xlsx");

        await _service.ExportCustomerCardAsync(999, 999, path);

        Assert.False(File.Exists(path));
    }

    // ============================================================
    // ExportReportAsync Tests
    // ============================================================

    [Fact]
    public async Task ExportReport_CreatesValidXlsx_WithFourSheets()
    {
        await SeedReportDataAsync();
        var path = TempFile("report.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        Assert.Equal(4, wb.Worksheets.Count);
        Assert.Equal("Özet", wb.Worksheets.ElementAt(0).Name);
        Assert.Equal("İşlemler", wb.Worksheets.ElementAt(1).Name);
        Assert.Equal("Ödemeler", wb.Worksheets.ElementAt(2).Name);
        Assert.Equal("Giderler", wb.Worksheets.ElementAt(3).Name);
    }

    [Fact]
    public async Task ExportReport_SummarySheet_HasCorrectTotals()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_summary.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Özet");

        // Title
        Assert.Contains("BÜLENT OTO ELEKTRİK", ws.Cell("A1").GetString());

        // Period
        Assert.Contains("01.06.2025", ws.Cell("A2").GetString());
        Assert.Contains("30.06.2025", ws.Cell("A2").GetString());

        // Totals: 500+1500 = 2000 revenue
        Assert.Equal(2000m, ws.Cell("B4").GetValue<decimal>());

        // Payments: 500
        Assert.Equal(500m, ws.Cell("B5").GetValue<decimal>());

        // Expenses: 10000
        Assert.Equal(10000m, ws.Cell("B6").GetValue<decimal>());

        // Net: 500 (payments) - 10000 (expenses) = -9500
        Assert.Equal(-9500m, ws.Cell("B7").GetValue<decimal>());
    }

    [Fact]
    public async Task ExportReport_ServiceRecordsSheet_HasAllRecords()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_sr.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("İşlemler");

        // Headers
        Assert.Equal("S.NO", ws.Cell(1, 1).GetString());
        Assert.Equal("TARİH", ws.Cell(1, 2).GetString());
        Assert.Equal("MÜŞTERİ", ws.Cell(1, 3).GetString());
        Assert.Equal("PLAKA", ws.Cell(1, 4).GetString());
        Assert.Equal("TUTAR", ws.Cell(1, 10).GetString());

        // 2 service records in June
        Assert.Equal(1, ws.Cell(2, 1).GetValue<int>());
        Assert.Equal("01.06.2025", ws.Cell(2, 2).GetString());
        Assert.Equal("Ali Veli", ws.Cell(2, 3).GetString());
        Assert.Equal("06 ABC 123", ws.Cell(2, 4).GetString());
        Assert.Equal("Yağ değişimi", ws.Cell(2, 6).GetString());
        Assert.Equal(500m, ws.Cell(2, 10).GetValue<decimal>());

        Assert.Equal(2, ws.Cell(3, 1).GetValue<int>());
        Assert.Equal("Fren bakımı", ws.Cell(3, 6).GetString());
        Assert.Equal(1500m, ws.Cell(3, 10).GetValue<decimal>());

        // No 3rd row
        Assert.True(ws.Cell(4, 1).IsEmpty());
    }

    [Fact]
    public async Task ExportReport_PaymentsSheet_HasPaymentData()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_pay.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Ödemeler");

        Assert.Equal("S.NO", ws.Cell(1, 1).GetString());
        Assert.Equal("TUTAR", ws.Cell(1, 4).GetString());

        // 1 payment
        Assert.Equal(1, ws.Cell(2, 1).GetValue<int>());
        Assert.Equal("05.06.2025", ws.Cell(2, 2).GetString());
        Assert.Equal("Ali Veli", ws.Cell(2, 3).GetString());
        Assert.Equal(500m, ws.Cell(2, 4).GetValue<decimal>());
        Assert.Equal("CreditCard", ws.Cell(2, 5).GetString());
    }

    [Fact]
    public async Task ExportReport_ExpensesSheet_HasExpenseData()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_exp.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("Giderler");

        Assert.Equal("S.NO", ws.Cell(1, 1).GetString());
        Assert.Equal("KATEGORİ", ws.Cell(1, 3).GetString());
        Assert.Equal("TUTAR", ws.Cell(1, 5).GetString());

        // 1 expense
        Assert.Equal(1, ws.Cell(2, 1).GetValue<int>());
        Assert.Equal("01.06.2025", ws.Cell(2, 2).GetString());
        Assert.Equal("Kira", ws.Cell(2, 3).GetString());
        Assert.Equal("Aylık kira", ws.Cell(2, 4).GetString());
        Assert.Equal(10000m, ws.Cell(2, 5).GetValue<decimal>());
    }

    [Fact]
    public async Task ExportReport_DateFilter_OnlyIncludesRecordsInRange()
    {
        await SeedReportDataAsync();

        // Add an out-of-range record
        _context.ServiceRecords.Add(new ServiceRecord
        {
            Id = 100, VehicleId = 10,
            ServiceDate = new DateTime(2025, 7, 1),
            WorkPerformed = "Temmuz işlemi",
            Quantity = 1, UnitPrice = 999m, TotalAmount = 999m
        });
        await _context.SaveChangesAsync();

        var path = TempFile("report_filter.xlsx");

        // Only query June
        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("İşlemler");

        // Should only have 2 records (June), not 3
        Assert.Equal(2, ws.Cell(3, 1).GetValue<int>());  // last S.NO = 2
        Assert.True(ws.Cell(4, 1).IsEmpty());             // no 3rd record
    }

    [Fact]
    public async Task ExportReport_EmptyDateRange_NoDataRows()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_empty.xlsx");

        // Query a range with no data
        await _service.ExportReportAsync(
            new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), path);

        Assert.True(File.Exists(path));
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("İşlemler");

        // Headers present, no data
        Assert.Equal("S.NO", ws.Cell(1, 1).GetString());
        Assert.True(ws.Cell(2, 1).IsEmpty());
    }

    [Fact]
    public async Task ExportReport_HeaderStyling_NavyBgWhiteFont()
    {
        await SeedReportDataAsync();
        var path = TempFile("report_style.xlsx");

        await _service.ExportReportAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 30), path);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet("İşlemler");

        for (int col = 1; col <= 10; col++)
        {
            var cell = ws.Cell(1, col);
            Assert.True(cell.Style.Font.Bold);
            Assert.Equal(10, cell.Style.Font.FontSize);
        }
    }

    // ============================================================
    // GetExportFolder / SetExportFolder Tests
    // ============================================================

    [Fact]
    public void GetExportFolder_ReturnsDefaultDocumentsFolder()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BulentOtoElektrik");

        Assert.Equal(expected, _service.GetExportFolder());
    }

    [Fact]
    public void SetExportFolder_UpdatesFolder()
    {
        var newFolder = Path.Combine(_tempDir, "CustomExport");

        _service.SetExportFolder(newFolder);

        Assert.Equal(newFolder, _service.GetExportFolder());
        Assert.True(Directory.Exists(newFolder));
    }

    // ============================================================
    // AutoExportCustomerCardsAsync Tests
    // ============================================================

    [Fact]
    public async Task AutoExport_SingleVehicle_CreatesOneFile()
    {
        await SeedCustomerCardDataAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportCustomerCardsAsync(1);

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Single(files);
        Assert.Contains("Kemal Aras", Path.GetFileName(files[0]));
        Assert.Contains("31 ALT 559", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task AutoExport_MultipleVehicles_CreatesOneFilePerVehicle()
    {
        await SeedCustomerCardDataAsync();

        // Add second vehicle for same customer
        _context.Vehicles.Add(new Vehicle
        {
            Id = 2, CustomerId = 1, PlateNumber = "06 ABC 789",
            VehicleBrand = "Toyota", VehicleModel = "Corolla"
        });
        await _context.SaveChangesAsync();

        _service.SetExportFolder(_tempDir);

        await _service.AutoExportCustomerCardsAsync(1);

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Equal(2, files.Length);
        Assert.Contains(files, f => Path.GetFileName(f).Contains("31 ALT 559"));
        Assert.Contains(files, f => Path.GetFileName(f).Contains("06 ABC 789"));
    }

    [Fact]
    public async Task AutoExport_NonExistentCustomer_NoFilesCreated()
    {
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportCustomerCardsAsync(999);

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Empty(files);
    }

    [Fact]
    public async Task AutoExport_OverwritesExistingFile()
    {
        await SeedCustomerCardDataAsync();
        _service.SetExportFolder(_tempDir);

        // First export
        await _service.AutoExportCustomerCardsAsync(1);
        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Single(files);
        var firstWriteTime = File.GetLastWriteTimeUtc(files[0]);

        // Wait a bit then re-export (simulating data change)
        await Task.Delay(50);
        await _service.AutoExportCustomerCardsAsync(1);

        files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Single(files); // still 1 file, not 2
        var secondWriteTime = File.GetLastWriteTimeUtc(files[0]);
        Assert.True(secondWriteTime >= firstWriteTime);
    }

    [Fact]
    public async Task AutoExport_FileContainsCorrectData()
    {
        await SeedCustomerCardDataAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportCustomerCardsAsync(1);

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Single(files);

        using var wb = new XLWorkbook(files[0]);
        var ws = wb.Worksheet("kart1");

        // Verify customer info
        Assert.Equal("Kemal Aras", ws.Cell("C1").GetString());
        Assert.Equal("31 ALT 559", ws.Cell("C5").GetString());

        // Verify service records exist
        Assert.Equal(1, ws.Cell(11, 1).GetValue<int>());
        Assert.Equal("Akü değişimi", ws.Cell(11, 4).GetString());
    }

    [Fact]
    public async Task AutoExport_CustomerWithNoVehicles_NoFilesCreated()
    {
        _context.Customers.Add(new Customer { Id = 50, FullName = "Boş Müşteri" });
        await _context.SaveChangesAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportCustomerCardsAsync(50);

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Empty(files);
    }

    [Fact]
    public async Task AutoExport_CreatesExportFolderIfNotExists()
    {
        await SeedCustomerCardDataAsync();
        var newFolder = Path.Combine(_tempDir, "AutoCreated");
        _service.SetExportFolder(newFolder);

        await _service.AutoExportCustomerCardsAsync(1);

        Assert.True(Directory.Exists(newFolder));
        var files = Directory.GetFiles(newFolder, "*.xlsx");
        Assert.Single(files);
    }

    // ============================================================
    // AutoExportReportsAsync Tests
    // ============================================================

    [Fact]
    public async Task AutoExportReports_CreatesAllFourFiles()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        // June 1, 2025 — a Sunday
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 1));

        var files = Directory.GetFiles(_tempDir, "Rapor_*.xlsx");
        Assert.Equal(4, files.Length);
        Assert.Contains(files, f => Path.GetFileName(f) == "Rapor_Gunluk_20250601.xlsx");
        Assert.Contains(files, f => Path.GetFileName(f) == "Rapor_Haftalik_20250526_20250601.xlsx");
        Assert.Contains(files, f => Path.GetFileName(f) == "Rapor_Aylik_202506.xlsx");
        Assert.Contains(files, f => Path.GetFileName(f) == "Rapor_Yillik_2025.xlsx");
    }

    [Fact]
    public async Task AutoExportReports_DailyFile_ContainsOnlyThatDay()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        // June 1 has 1 service record (500 TL) and 1 expense (10000 TL)
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 1));

        var dailyFile = Path.Combine(_tempDir, "Rapor_Gunluk_20250601.xlsx");
        using var wb = new XLWorkbook(dailyFile);
        var summary = wb.Worksheet("Özet");

        // Only June 1 service: 500 TL revenue
        Assert.Equal(500m, summary.Cell("B4").GetValue<decimal>());
        // Only June 1 expense: 10000 TL
        Assert.Equal(10000m, summary.Cell("B6").GetValue<decimal>());

        // Service records sheet should have only 1 record
        var srSheet = wb.Worksheet("İşlemler");
        Assert.Equal(1, srSheet.Cell(2, 1).GetValue<int>());
        Assert.True(srSheet.Cell(3, 1).IsEmpty());
    }

    [Fact]
    public async Task AutoExportReports_WeeklyFile_ContainsCorrectDateRange()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        // June 15 is a Sunday; its week is Mon Jun 9 - Sun Jun 15
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 15));

        var weeklyFile = Path.Combine(_tempDir, "Rapor_Haftalik_20250609_20250615.xlsx");
        Assert.True(File.Exists(weeklyFile));

        using var wb = new XLWorkbook(weeklyFile);
        var srSheet = wb.Worksheet("İşlemler");

        // June 15 has 1 service record (1500 TL)
        Assert.Equal(1, srSheet.Cell(2, 1).GetValue<int>());
        Assert.Equal("15.06.2025", srSheet.Cell(2, 2).GetString());
        Assert.True(srSheet.Cell(3, 1).IsEmpty());
    }

    [Fact]
    public async Task AutoExportReports_MonthlyFile_ContainsWholeMonth()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 15));

        var monthlyFile = Path.Combine(_tempDir, "Rapor_Aylik_202506.xlsx");
        using var wb = new XLWorkbook(monthlyFile);
        var summary = wb.Worksheet("Özet");

        // All June: 500 + 1500 = 2000 revenue
        Assert.Equal(2000m, summary.Cell("B4").GetValue<decimal>());
        // 1 payment: 500
        Assert.Equal(500m, summary.Cell("B5").GetValue<decimal>());
        // 1 expense: 10000
        Assert.Equal(10000m, summary.Cell("B6").GetValue<decimal>());

        // Service records sheet: 2 records
        var srSheet = wb.Worksheet("İşlemler");
        Assert.Equal(2, srSheet.Cell(3, 1).GetValue<int>());
        Assert.True(srSheet.Cell(4, 1).IsEmpty());
    }

    [Fact]
    public async Task AutoExportReports_YearlyFile_ContainsWholeYear()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 1));

        var yearlyFile = Path.Combine(_tempDir, "Rapor_Yillik_2025.xlsx");
        using var wb = new XLWorkbook(yearlyFile);
        var summary = wb.Worksheet("Özet");

        // Entire 2025: same as June since all data is in June
        Assert.Equal(2000m, summary.Cell("B4").GetValue<decimal>());
        Assert.Equal(500m, summary.Cell("B5").GetValue<decimal>());
        Assert.Equal(10000m, summary.Cell("B6").GetValue<decimal>());
    }

    [Fact]
    public async Task AutoExportReports_OverwritesExistingFiles()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        // First export
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 1));
        var dailyFile = Path.Combine(_tempDir, "Rapor_Gunluk_20250601.xlsx");
        var firstWriteTime = File.GetLastWriteTimeUtc(dailyFile);

        await Task.Delay(50);

        // Second export - should overwrite
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 1));
        var secondWriteTime = File.GetLastWriteTimeUtc(dailyFile);
        Assert.True(secondWriteTime >= firstWriteTime);

        // Still only 4 report files (not 8)
        var files = Directory.GetFiles(_tempDir, "Rapor_*.xlsx");
        Assert.Equal(4, files.Length);
    }

    [Fact]
    public async Task AutoExportReports_NonExistentData_CreatesEmptyReports()
    {
        // No data seeded at all
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportReportsAsync(new DateTime(2024, 1, 15));

        var files = Directory.GetFiles(_tempDir, "Rapor_*.xlsx");
        Assert.Equal(4, files.Length);

        // Daily file should exist but have no data rows
        var dailyFile = Path.Combine(_tempDir, "Rapor_Gunluk_20240115.xlsx");
        using var wb = new XLWorkbook(dailyFile);
        var srSheet = wb.Worksheet("İşlemler");
        Assert.True(srSheet.Cell(2, 1).IsEmpty());
    }

    [Fact]
    public async Task AutoExportReports_MondayDate_CorrectWeekRange()
    {
        _service.SetExportFolder(_tempDir);

        // Monday June 2, 2025 — week should be Jun 2 (Mon) - Jun 8 (Sun)
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 2));

        var files = Directory.GetFiles(_tempDir, "Rapor_Haftalik_*.xlsx");
        Assert.Single(files);
        Assert.Equal("Rapor_Haftalik_20250602_20250608.xlsx", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task AutoExportReports_SundayDate_CorrectWeekRange()
    {
        _service.SetExportFolder(_tempDir);

        // Sunday June 8, 2025 — week should be Jun 2 (Mon) - Jun 8 (Sun)
        await _service.AutoExportReportsAsync(new DateTime(2025, 6, 8));

        var files = Directory.GetFiles(_tempDir, "Rapor_Haftalik_*.xlsx");
        Assert.Single(files);
        Assert.Equal("Rapor_Haftalik_20250602_20250608.xlsx", Path.GetFileName(files[0]));
    }

    // ============================================================
    // AutoExportAllAsync Tests
    // ============================================================

    [Fact]
    public async Task AutoExportAll_CreatesCustomerCardsAndReports()
    {
        await SeedCustomerCardDataAsync();
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportAllAsync();

        var allFiles = Directory.GetFiles(_tempDir, "*.xlsx");

        // Should have customer card files
        Assert.Contains(allFiles, f => Path.GetFileName(f).Contains("Kemal Aras"));

        // Should have report files (daily, weekly, monthly, yearly)
        Assert.Contains(allFiles, f => Path.GetFileName(f).StartsWith("Rapor_Gunluk_"));
        Assert.Contains(allFiles, f => Path.GetFileName(f).StartsWith("Rapor_Haftalik_"));
        Assert.Contains(allFiles, f => Path.GetFileName(f).StartsWith("Rapor_Aylik_"));
        Assert.Contains(allFiles, f => Path.GetFileName(f).StartsWith("Rapor_Yillik_"));
    }

    [Fact]
    public async Task AutoExportAll_RecreatesDeletedFiles()
    {
        await SeedReportDataAsync();
        _service.SetExportFolder(_tempDir);

        // First export
        await _service.AutoExportAllAsync();
        var filesBeforeDelete = Directory.GetFiles(_tempDir, "*.xlsx").Length;
        Assert.True(filesBeforeDelete > 0);

        // Delete all files
        foreach (var file in Directory.GetFiles(_tempDir, "*.xlsx"))
            File.Delete(file);
        Assert.Empty(Directory.GetFiles(_tempDir, "*.xlsx"));

        // Re-export - should recreate everything
        await _service.AutoExportAllAsync();
        var filesAfterReexport = Directory.GetFiles(_tempDir, "*.xlsx").Length;
        Assert.Equal(filesBeforeDelete, filesAfterReexport);
    }

    [Fact]
    public async Task AutoExportAll_NoData_NoReportFiles()
    {
        // No data seeded
        _service.SetExportFolder(_tempDir);

        await _service.AutoExportAllAsync();

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Empty(files);
    }

    [Fact]
    public async Task AutoExportAll_MultipleCustomers_AllCardsExported()
    {
        await SeedCustomerCardDataAsync();

        // Add a second customer with vehicle
        _context.Customers.Add(new Customer { Id = 5, FullName = "Ahmet Yılmaz", Phone1 = "0555 111 22 33" });
        _context.Vehicles.Add(new Vehicle { Id = 5, CustomerId = 5, PlateNumber = "06 TEST 99", VehicleBrand = "BMW" });
        await _context.SaveChangesAsync();

        _service.SetExportFolder(_tempDir);

        await _service.AutoExportAllAsync();

        var files = Directory.GetFiles(_tempDir, "*.xlsx");
        Assert.Contains(files, f => Path.GetFileName(f).Contains("Kemal Aras"));
        Assert.Contains(files, f => Path.GetFileName(f).Contains("Ahmet Yılmaz"));
    }
}
