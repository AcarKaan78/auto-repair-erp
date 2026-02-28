using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Infrastructure.Data;
using BulentOtoElektrik.Infrastructure.Services;
using BulentOtoElektrik.Tests.Helpers;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Tests.Services;

public class ExcelImportServiceTests : IDisposable
{
    private readonly string _dbName;
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelImportService _service;
    private readonly string _tempDir;

    public ExcelImportServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        _serviceProvider = services.BuildServiceProvider();
        _service = new ExcelImportService(_serviceProvider);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ExcelImportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Ensure DB is created
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    private AppDbContext CreateContext()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    #region Format Detection Tests

    [Fact]
    public async Task ParseFile_OldFormat_DetectsCorrectly()
    {
        var path = CreateOldFormatFile("TEST CUSTOMER", "34 ABC 123", "FORD TRANSIT");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal(ExcelFormatType.OldFormat, result.Format);
    }

    [Fact]
    public async Task ParseFile_AppFormat_DetectsCorrectly()
    {
        var path = CreateAppFormatFile("TEST CUSTOMER", "34 ABC 123", "FORD", "TRANSIT");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal(ExcelFormatType.AppFormat, result.Format);
    }

    [Fact]
    public async Task ParseFile_UnknownFormat_ReturnsError()
    {
        var path = CreateEmptyFile();
        var result = await _service.ParseFileAsync(path);

        Assert.False(result.Success);
        Assert.Equal(ExcelFormatType.Unknown, result.Format);
    }

    #endregion

    #region OLD Format Parsing Tests

    [Fact]
    public async Task ParseFile_OldFormat_ParsesCustomerMetadata()
    {
        var path = CreateOldFormatFile("KEMAL ARAS", "31 ALT 559", "FORD TRANSIT",
            phone1: "5551234567", phone2: "5559876543", kimlik: "12345678901",
            email: "test@test.com", address: "Ankara");

        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal("KEMAL ARAS", result.CustomerName);
        Assert.Equal("31 ALT 559", result.PlateNumber);
        Assert.Equal("5551234567", result.Phone1);
        Assert.Equal("5559876543", result.Phone2);
        Assert.Equal("12345678901", result.IdentityNumber);
        Assert.Equal("test@test.com", result.Email);
        Assert.Equal("Ankara", result.Address);
    }

    [Fact]
    public async Task ParseFile_OldFormat_SplitsVehicleBrandModel()
    {
        var path = CreateOldFormatFile("TEST", "34 ABC 123", "FORD TRANSIT");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal("FORD", result.VehicleBrand);
        Assert.Equal("TRANSIT", result.VehicleModel);
    }

    [Fact]
    public async Task ParseFile_OldFormat_SingleBrandNoBrand()
    {
        var path = CreateOldFormatFile("TEST", "34 ABC 123", "FORD");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal("FORD", result.VehicleBrand);
        Assert.Null(result.VehicleModel);
    }

    [Fact]
    public async Task ParseFile_OldFormat_ParsesServiceRows()
    {
        var path = CreateOldFormatFile("TEST", "34 ABC 123", "FORD TRANSIT",
            serviceRows: new[]
            {
                (new DateTime(2025, 1, 15), "Arıza", "Marş motoru değiştirildi", "İRFAN USTA", 1, 500m),
                (new DateTime(2025, 2, 20), "Akü", "Akü değişimi", "ARDA K.", 1, 1500m)
            });

        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal(2, result.ServiceRows.Count);
        Assert.Equal(new DateTime(2025, 1, 15), result.ServiceRows[0].ServiceDate);
        Assert.Equal("Arıza", result.ServiceRows[0].Complaint);
        Assert.Equal("Marş motoru değiştirildi", result.ServiceRows[0].WorkPerformed);
        Assert.Equal("İRFAN USTA", result.ServiceRows[0].TechnicianName);
        Assert.Equal(1, result.ServiceRows[0].Quantity);
        Assert.Equal(500m, result.ServiceRows[0].UnitPrice);
    }

    [Fact]
    public async Task ParseFile_OldFormat_SkipsEmptyDateRows()
    {
        // Create file with 2 real rows and empty template rows between
        var path = CreateOldFormatFileWithEmptyRows("TEST", "34 ABC 123", "FORD TRANSIT");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal(2, result.ServiceRows.Count);
    }

    [Fact]
    public async Task ParseFile_OldFormat_ParsesPaymentColumn()
    {
        var path = CreateOldFormatFileWithPayments("TEST", "34 ABC 123", "FORD TRANSIT");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Single(result.PaymentRows);
        Assert.Equal(3000m, result.PaymentRows[0].Amount);
    }

    [Fact]
    public async Task ParseFile_OldFormat_ParsesCurrency()
    {
        var path = CreateOldFormatFile("TEST", "34 ABC 123", "FORD", currency: "USD");
        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal(CurrencyType.TL, result.Currency);
    }

    #endregion

    #region APP Format Parsing Tests

    [Fact]
    public async Task ParseFile_AppFormat_ParsesCustomerMetadata()
    {
        var path = CreateAppFormatFile("AHMET YILMAZ", "42 ACR 062", "FORD", "FOCUS",
            phone1: "5551111111", year: 2020);

        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Equal("AHMET YILMAZ", result.CustomerName);
        Assert.Equal("42 ACR 062", result.PlateNumber);
        Assert.Equal("5551111111", result.Phone1);
        Assert.Equal("FORD", result.VehicleBrand);
        Assert.Equal("FOCUS", result.VehicleModel);
        Assert.Equal(2020, result.VehicleYear);
    }

    [Fact]
    public async Task ParseFile_AppFormat_ParsesServiceRowsWithNotes()
    {
        var path = CreateAppFormatFile("TEST", "34 ABC 123", "FORD", "FOCUS",
            serviceRows: new[]
            {
                ("15.01.2025", "Arıza", "Marş değişimi", "İRFAN", 1, 500m, "Test notu")
            });

        var result = await _service.ParseFileAsync(path);

        Assert.True(result.Success);
        Assert.Single(result.ServiceRows);
        Assert.Equal("Test notu", result.ServiceRows[0].Notes);
    }

    #endregion

    #region Import (Persistence) Tests

    [Fact]
    public async Task ImportFiles_CreatesCustomerAndVehicle()
    {
        var path = CreateOldFormatFile("YENI MUSTERI", "99 YEN 001", "BMW 320",
            serviceRows: new[]
            {
                (new DateTime(2025, 3, 1), "Test", "Test işlem", "TEKNİSYEN", 1, 200m)
            });

        var result = await _service.ImportFilesAsync(new[] { path }, "skip");

        Assert.Equal(1, result.SuccessfulFiles);
        Assert.Equal(1, result.CustomersCreated);
        Assert.Equal(1, result.VehiclesCreated);
        Assert.Equal(1, result.ServiceRecordsCreated);

        using var ctx = CreateContext();
        var customer = await ctx.Customers.FirstOrDefaultAsync(c => c.FullName == "YENI MUSTERI");
        Assert.NotNull(customer);

        var vehicle = await ctx.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == "99 YEN 001");
        Assert.NotNull(vehicle);
        Assert.Equal("BMW", vehicle.VehicleBrand);
        Assert.Equal("320", vehicle.VehicleModel);
    }

    [Fact]
    public async Task ImportFiles_DuplicatePlate_Skip_SkipsFile()
    {
        // Pre-create a vehicle
        using (var ctx = CreateContext())
        {
            var customer = new Customer { FullName = "EXISTING" };
            ctx.Customers.Add(customer);
            await ctx.SaveChangesAsync();
            ctx.Vehicles.Add(new Vehicle { CustomerId = customer.Id, PlateNumber = "34 DUP 001" });
            await ctx.SaveChangesAsync();
        }

        var path = CreateOldFormatFile("EXISTING", "34 DUP 001", "FORD TRANSIT",
            serviceRows: new[]
            {
                (new DateTime(2025, 1, 1), "Test", "Test work", "TECH", 1, 100m)
            });

        var result = await _service.ImportFilesAsync(new[] { path }, "skip");

        Assert.Equal(1, result.VehiclesSkipped);
        Assert.Equal(0, result.ServiceRecordsCreated);
    }

    [Fact]
    public async Task ImportFiles_DuplicatePlate_Merge_AddsRecords()
    {
        int vehicleId;
        using (var ctx = CreateContext())
        {
            var customer = new Customer { FullName = "MERGE TEST" };
            ctx.Customers.Add(customer);
            await ctx.SaveChangesAsync();
            var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "34 MRG 001" };
            ctx.Vehicles.Add(vehicle);
            await ctx.SaveChangesAsync();
            vehicleId = vehicle.Id;
        }

        var path = CreateOldFormatFile("MERGE TEST", "34 MRG 001", "FORD TRANSIT",
            serviceRows: new[]
            {
                (new DateTime(2025, 5, 1), "New complaint", "New work", "TECH", 1, 300m)
            });

        var result = await _service.ImportFilesAsync(new[] { path }, "merge");

        Assert.Equal(1, result.VehiclesMerged);
        Assert.Equal(1, result.ServiceRecordsCreated);

        using var verifyCtx = CreateContext();
        var records = await verifyCtx.ServiceRecords.Where(sr => sr.VehicleId == vehicleId).ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task ImportFiles_CreatesNewTechnician()
    {
        var path = CreateOldFormatFile("TECH TEST", "34 TCH 001", "FORD FOCUS",
            serviceRows: new[]
            {
                (new DateTime(2025, 1, 1), "Test", "Test work", "YENİ TEKNİSYEN", 1, 100m)
            });

        var result = await _service.ImportFilesAsync(new[] { path }, "skip");

        Assert.Equal(1, result.TechniciansCreated);

        using var ctx = CreateContext();
        var tech = await ctx.Technicians.FirstOrDefaultAsync(t => t.FullName == "YENİ TEKNİSYEN");
        Assert.NotNull(tech);
        Assert.True(tech.IsActive);
    }

    [Fact]
    public async Task ImportFiles_MatchesExistingTechnician()
    {
        // Pre-create technician
        using (var setupCtx = CreateContext())
        {
            setupCtx.Technicians.Add(new Technician { FullName = "İRFAN USTA", IsActive = true });
            await setupCtx.SaveChangesAsync();
        }

        var path = CreateOldFormatFile("MATCH TEST", "34 MCH 001", "FORD TRANSIT",
            serviceRows: new[]
            {
                (new DateTime(2025, 1, 1), "Test", "Test work", "İRFAN USTA", 1, 100m)
            });

        var result = await _service.ImportFilesAsync(new[] { path }, "skip");

        Assert.Equal(0, result.TechniciansCreated);

        using var verifyCtx = CreateContext();
        var record = await verifyCtx.ServiceRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);
        Assert.NotNull(record.TechnicianId);
    }

    [Fact]
    public async Task ImportFiles_InvalidFile_CountsAsError()
    {
        var badPath = Path.Combine(_tempDir, "bad.xlsx");
        File.WriteAllText(badPath, "not an xlsx file");

        var result = await _service.ImportFilesAsync(new[] { badPath }, "skip");

        Assert.Equal(1, result.FailedFiles);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ImportFiles_PaymentsFromOldFormat()
    {
        var path = CreateOldFormatFileWithPayments("PAY TEST", "34 PAY 001", "FORD FOCUS");

        var result = await _service.ImportFilesAsync(new[] { path }, "skip");

        Assert.Equal(1, result.PaymentsCreated);

        using var ctx = CreateContext();
        var payment = await ctx.Payments.FirstOrDefaultAsync();
        Assert.NotNull(payment);
        Assert.Equal(3000m, payment.Amount);
        Assert.Equal(PaymentMethod.Cash, payment.PaymentMethod);
    }

    #endregion

    #region Helper Methods - Create Test Excel Files

    private string CreateOldFormatFile(
        string customerName, string plate, string vehicleModel,
        string? phone1 = null, string? phone2 = null, string? kimlik = null,
        string? email = null, string? address = null, string? currency = "TL",
        (DateTime date, string complaint, string work, string tech, int qty, decimal price)[]? serviceRows = null)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("kart1");

        // Header rows (OLD format order)
        ws.Range("A1:B1").Merge().SetValue("ADI SOYADI");
        ws.Range("C1:H1").Merge().SetValue(customerName);
        ws.Range("A2:B2").Merge().SetValue("PLAKA NO");
        ws.Range("C2:H2").Merge().SetValue(plate);
        ws.Range("A3:B3").Merge().SetValue("ARAC MODELI");
        ws.Range("C3:H3").Merge().SetValue(vehicleModel);
        ws.Range("A4:B4").Merge().SetValue("CEP 1");
        ws.Range("C4:H4").Merge().SetValue(phone1 ?? "");
        ws.Range("A5:B5").Merge().SetValue("CEP 2");
        ws.Range("C5:H5").Merge().SetValue(phone2 ?? "");
        ws.Range("A6:B6").Merge().SetValue("KIMLIK NO");
        ws.Range("C6:H6").Merge().SetValue(kimlik ?? "");
        ws.Range("A7:B7").Merge().SetValue("E-Mail Adresi");
        ws.Range("C7:H7").Merge().SetValue(email ?? "");
        ws.Range("A8:B8").Merge().SetValue("ADRES");
        ws.Range("C8:H8").Merge().SetValue(address ?? "");

        // Currency
        ws.Cell("I1").Value = "KULLANILAN PARA BIRIMI";
        ws.Cell("J1").Value = currency;

        // Banner
        ws.Range("A9:J9").Merge().SetValue("BULENT OTO ELEKTRIK");

        // Column headers (OLD format)
        ws.Cell("A10").Value = "SIRA";
        ws.Cell("B10").Value = "TARIH";
        ws.Cell("C10").Value = "ARIZA SIKAYET";
        ws.Cell("D10").Value = "YAPILAN ISLEM";
        ws.Cell("E10").Value = "TEKNISYEN";
        ws.Cell("F10").Value = "MIKTAR";
        ws.Cell("G10").Value = "BIRIM FIYAT";
        ws.Cell("H10").Value = "TUTAR(BORC)";
        ws.Cell("I10").Value = "ODEME";
        ws.Cell("J10").Value = "KALAN BAKIYE";

        // Data rows
        if (serviceRows != null)
        {
            for (int i = 0; i < serviceRows.Length; i++)
            {
                var sr = serviceRows[i];
                int row = 11 + i;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = sr.date;
                ws.Cell(row, 3).Value = sr.complaint;
                ws.Cell(row, 4).Value = sr.work;
                ws.Cell(row, 5).Value = sr.tech;
                ws.Cell(row, 6).Value = sr.qty;
                ws.Cell(row, 7).Value = sr.price;
                ws.Cell(row, 8).Value = sr.qty * sr.price; // TUTAR
                ws.Cell(row, 9).Value = 0; // ODEME
            }
        }

        workbook.SaveAs(path);
        return path;
    }

    private string CreateOldFormatFileWithEmptyRows(string customerName, string plate, string vehicleModel)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("kart1");

        ws.Range("A1:B1").Merge().SetValue("ADI SOYADI");
        ws.Range("C1:H1").Merge().SetValue(customerName);
        ws.Range("A2:B2").Merge().SetValue("PLAKA NO");
        ws.Range("C2:H2").Merge().SetValue(plate);
        ws.Range("A3:B3").Merge().SetValue("ARAC MODELI");
        ws.Range("C3:H3").Merge().SetValue(vehicleModel);
        for (int r = 4; r <= 8; r++)
        {
            ws.Range(r, 1, r, 2).Merge().SetValue("");
            ws.Range(r, 3, r, 8).Merge().SetValue("");
        }

        ws.Cell("I1").Value = "KULLANILAN PARA BIRIMI";
        ws.Cell("J1").Value = "TL";
        ws.Range("A9:J9").Merge().SetValue("BULENT OTO ELEKTRIK");
        ws.Cell("A10").Value = "SIRA";

        // Row 11: real data
        ws.Cell(11, 1).Value = 1;
        ws.Cell(11, 2).Value = new DateTime(2025, 1, 1);
        ws.Cell(11, 3).Value = "Complaint 1";
        ws.Cell(11, 4).Value = "Work 1";
        ws.Cell(11, 6).Value = 1;
        ws.Cell(11, 7).Value = 100;
        ws.Cell(11, 8).Value = 100;

        // Row 12-14: empty template rows (just row numbers, no date)
        ws.Cell(12, 1).Value = 2;
        ws.Cell(13, 1).Value = 3;
        ws.Cell(14, 1).Value = 4;

        // Row 15: real data
        ws.Cell(15, 1).Value = 5;
        ws.Cell(15, 2).Value = new DateTime(2025, 2, 1);
        ws.Cell(15, 3).Value = "Complaint 2";
        ws.Cell(15, 4).Value = "Work 2";
        ws.Cell(15, 6).Value = 2;
        ws.Cell(15, 7).Value = 50;
        ws.Cell(15, 8).Value = 100;

        workbook.SaveAs(path);
        return path;
    }

    private string CreateOldFormatFileWithPayments(string customerName, string plate, string vehicleModel)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("kart1");

        ws.Range("A1:B1").Merge().SetValue("ADI SOYADI");
        ws.Range("C1:H1").Merge().SetValue(customerName);
        ws.Range("A2:B2").Merge().SetValue("PLAKA NO");
        ws.Range("C2:H2").Merge().SetValue(plate);
        ws.Range("A3:B3").Merge().SetValue("ARAC MODELI");
        ws.Range("C3:H3").Merge().SetValue(vehicleModel);
        for (int r = 4; r <= 8; r++)
        {
            ws.Range(r, 1, r, 2).Merge().SetValue("");
            ws.Range(r, 3, r, 8).Merge().SetValue("");
        }

        ws.Cell("I1").Value = "KULLANILAN PARA BIRIMI";
        ws.Cell("J1").Value = "TL";
        ws.Range("A9:J9").Merge().SetValue("BULENT OTO ELEKTRIK");
        ws.Cell("A10").Value = "SIRA";

        // Service row with payment
        ws.Cell(11, 1).Value = 1;
        ws.Cell(11, 2).Value = new DateTime(2025, 1, 15);
        ws.Cell(11, 3).Value = "Arıza";
        ws.Cell(11, 4).Value = "Tamir yapıldı";
        ws.Cell(11, 6).Value = 1;
        ws.Cell(11, 7).Value = 5000;
        ws.Cell(11, 8).Value = 5000;
        ws.Cell(11, 9).Value = 3000; // ODEME column

        workbook.SaveAs(path);
        return path;
    }

    private string CreateAppFormatFile(
        string customerName, string plate, string brand, string model,
        string? phone1 = null, int? year = null,
        (string date, string complaint, string work, string tech, int qty, decimal price, string notes)[]? serviceRows = null)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("kart1");

        // APP format header rows
        ws.Range("A1:B1").Merge().SetValue("AD-SOYAD");
        ws.Range("C1:H1").Merge().SetValue(customerName);
        ws.Range("A2:B2").Merge().SetValue("TELEFON 1");
        ws.Range("C2:H2").Merge().SetValue(phone1 ?? "");
        ws.Range("A3:B3").Merge().SetValue("TELEFON 2");
        ws.Range("C3:H3").Merge().SetValue("");
        ws.Range("A4:B4").Merge().SetValue("T.C NO");
        ws.Range("C4:H4").Merge().SetValue("");
        ws.Range("A5:B5").Merge().SetValue("PLAKA");
        ws.Range("C5:H5").Merge().SetValue(plate);
        ws.Range("A6:B6").Merge().SetValue("MARKA");
        ws.Range("C6:H6").Merge().SetValue(brand);
        ws.Range("A7:B7").Merge().SetValue("MODEL");
        ws.Range("C7:H7").Merge().SetValue(model);
        ws.Range("A8:B8").Merge().SetValue("YIL");
        ws.Range("C8:H8").Merge().SetValue(year?.ToString() ?? "");

        ws.Cell("I1").Value = "PARA BİRİMİ";
        ws.Cell("J1").Value = "TL";
        ws.Range("A9:J9").Merge().SetValue("BÜLENT OTO ELEKTRİK");

        // APP format column headers
        ws.Cell("A10").Value = "S.NO";
        ws.Cell("B10").Value = "TARİH";
        ws.Cell("C10").Value = "ŞİKAYET";
        ws.Cell("D10").Value = "YAPILAN İŞLEM";
        ws.Cell("E10").Value = "TEKNİSYEN";
        ws.Cell("F10").Value = "MİKTAR";
        ws.Cell("G10").Value = "BİRİM FİYAT";
        ws.Cell("H10").Value = "TUTAR";
        ws.Cell("I10").Value = "KALAN BAKİYE";
        ws.Cell("J10").Value = "NOTLAR";

        if (serviceRows != null)
        {
            for (int i = 0; i < serviceRows.Length; i++)
            {
                var sr = serviceRows[i];
                int row = 11 + i;
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = sr.date; // string date
                ws.Cell(row, 3).Value = sr.complaint;
                ws.Cell(row, 4).Value = sr.work;
                ws.Cell(row, 5).Value = sr.tech;
                ws.Cell(row, 6).Value = sr.qty;
                ws.Cell(row, 7).Value = sr.price;
                ws.Cell(row, 8).Value = sr.qty * sr.price;
                ws.Cell(row, 10).Value = sr.notes;
            }
        }

        workbook.SaveAs(path);
        return path;
    }

    private string CreateEmptyFile()
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Sheet1");
        workbook.SaveAs(path);
        return path;
    }

    #endregion
}
