using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly IServiceProvider _serviceProvider;
    private string _exportFolder;

    public ExcelExportService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _exportFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "BulentOtoElektrik");
    }

    public string GetExportFolder() => _exportFolder;

    public void SetExportFolder(string path)
    {
        _exportFolder = path;
        Directory.CreateDirectory(_exportFolder);
    }

    public async Task ExportCustomerCardAsync(int customerId, int vehicleId, string filePath, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var customer = await context.Customers
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer == null) return;

        var vehicle = await context.Vehicles
            .Include(v => v.ServiceRecords!)
                .ThenInclude(sr => sr.Technician)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, ct);

        if (vehicle == null) return;

        var serviceRecords = vehicle.ServiceRecords?
            .OrderBy(sr => sr.ServiceDate)
            .ThenBy(sr => sr.Id)
            .ToList() ?? new List<Core.Entities.ServiceRecord>();

        var vehiclePayments = customer.Payments?
            .Where(p => p.VehicleId == vehicleId || p.VehicleId == null)
            .Sum(p => p.Amount) ?? 0m;

        var totalDebt = serviceRecords.Sum(sr => sr.TotalAmount);

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("kart1");

            // Column widths
            ws.Column("A").Width = 5.71;
            ws.Column("B").Width = 11.86;
            ws.Column("C").Width = 24.57;
            ws.Column("D").Width = 27.71;
            ws.Column("E").Width = 15.43;
            ws.Column("F").Width = 6.71;
            ws.Column("G").Width = 12.14;
            ws.Column("H").Width = 14.14;
            ws.Column("I").Width = 22.57;
            ws.Column("J").Width = 22.29;

            // --- Rows 1-8 LEFT SIDE: Customer info ---
            SetLabelRow(ws, 1, "AD-SOYAD", customer.FullName, isRedValue: true);
            SetLabelRow(ws, 2, "TELEFON 1", customer.Phone1 ?? "", isRedValue: true);
            SetLabelRow(ws, 3, "TELEFON 2", customer.Phone2 ?? "", isRedValue: true);
            SetLabelRow(ws, 4, "T.C NO", customer.IdentityNumber ?? "", isRedValue: false);
            SetLabelRow(ws, 5, "PLAKA", vehicle.PlateNumber, isRedValue: false);
            SetLabelRow(ws, 6, "MARKA", vehicle.VehicleBrand ?? "", isRedValue: false);
            SetLabelRow(ws, 7, "MODEL", vehicle.VehicleModel ?? "", isRedValue: false);
            SetLabelRow(ws, 8, "YIL", vehicle.VehicleYear?.ToString() ?? "", isRedValue: false);

            // --- Rows 1-6 RIGHT SIDE: Summary ---
            var navyBlue = XLColor.FromHtml("#002060");

            // I1, J1 - Para Birimi
            ws.Cell("I1").Value = "PARA BİRİMİ";
            ws.Cell("I1").Style.Font.FontColor = navyBlue;
            ws.Cell("I1").Style.Font.Bold = true;
            ws.Cell("J1").Value = "TL";

            // I2, J2 - Borç
            ws.Cell("I2").Value = "BORÇ";
            ws.Cell("I2").Style.Font.FontColor = navyBlue;
            ws.Cell("I2").Style.Font.Bold = true;
            // J2 = SUM of TUTAR column (H11:H{lastRow})
            var dataStartRow = 11;
            var dataEndRow = dataStartRow + Math.Max(serviceRecords.Count - 1, 0);
            if (serviceRecords.Count > 0)
            {
                ws.Cell("J2").FormulaA1 = $"SUM(H{dataStartRow}:H{dataEndRow})";
            }
            else
            {
                ws.Cell("J2").Value = 0;
            }
            ws.Cell("J2").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

            // I3, J3 - Alacak
            ws.Cell("I3").Value = "ALACAK";
            ws.Cell("I3").Style.Font.FontColor = navyBlue;
            ws.Cell("I3").Style.Font.Bold = true;
            ws.Cell("J3").Value = vehiclePayments;
            ws.Cell("J3").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

            // I4 empty

            // I5, J5 - Toplam Borç
            ws.Cell("I5").Value = "TOPLAM BORÇ";
            ws.Cell("I5").Style.Font.FontColor = navyBlue;
            ws.Cell("I5").Style.Font.Bold = true;
            ws.Cell("J5").Value = totalDebt;
            ws.Cell("J5").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
            ws.Cell("J5").Style.Fill.BackgroundColor = XLColor.Red;
            ws.Cell("J5").Style.Font.FontColor = XLColor.White;
            ws.Cell("J5").Style.Font.Bold = true;

            // I6, J6 - Toplam Ödeme
            ws.Cell("I6").Value = "TOPLAM ÖDEME";
            ws.Cell("I6").Style.Font.FontColor = navyBlue;
            ws.Cell("I6").Style.Font.Bold = true;
            ws.Cell("J6").Value = vehiclePayments;
            ws.Cell("J6").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
            ws.Cell("J6").Style.Fill.BackgroundColor = XLColor.Red;
            ws.Cell("J6").Style.Font.FontColor = XLColor.White;
            ws.Cell("J6").Style.Font.Bold = true;

            // --- Row 9: Company banner ---
            ws.Range("A9:J9").Merge();
            ws.Cell("A9").Value = "BÜLENT OTO ELEKTRİK";
            ws.Cell("A9").Style.Font.FontSize = 28;
            ws.Cell("A9").Style.Font.Bold = true;
            ws.Cell("A9").Style.Font.FontColor = XLColor.FromHtml("#C00000");
            ws.Cell("A9").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFC7CE");
            ws.Cell("A9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A9").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(9).Height = 40;

            // --- Row 10: Headers ---
            var headers = new[] { "S.NO", "TARİH", "ŞİKAYET", "YAPILAN İŞLEM", "TEKNİSYEN", "MİKTAR", "BİRİM FİYAT", "TUTAR", "KALAN BAKİYE", "NOTLAR" };
            var headerBg = XLColor.FromHtml("#002060");

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(10, col);
                cell.Value = headers[col - 1];
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.White;
            }

            // --- Row 11+: Service records data ---
            decimal runningBalance = 0m;
            for (int i = 0; i < serviceRecords.Count; i++)
            {
                var sr = serviceRecords[i];
                int row = dataStartRow + i;

                // A = row number
                ws.Cell(row, 1).Value = i + 1;

                // B = ServiceDate
                ws.Cell(row, 2).Value = sr.ServiceDate.ToString("dd/MM/yyyy");

                // C = Complaint
                ws.Cell(row, 3).Value = sr.Complaint ?? "";

                // D = WorkPerformed
                ws.Cell(row, 4).Value = sr.WorkPerformed;

                // E = Technician FullName
                ws.Cell(row, 5).Value = sr.Technician?.FullName ?? "";

                // F = Quantity
                ws.Cell(row, 6).Value = sr.Quantity;

                // G = UnitPrice
                ws.Cell(row, 7).Value = sr.UnitPrice;
                ws.Cell(row, 7).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

                // H = TotalAmount (formula: =F*G)
                ws.Cell(row, 8).FormulaA1 = $"F{row}*G{row}";
                ws.Cell(row, 8).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

                // I = Running balance (cumulative sum of TUTAR)
                runningBalance += sr.TotalAmount;
                ws.Cell(row, 9).FormulaA1 = $"SUM(H{dataStartRow}:H{row})";
                ws.Cell(row, 9).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

                // J = Notes
                ws.Cell(row, 10).Value = sr.Notes ?? "";

                // Style all cells in this row
                for (int col = 1; col <= 10; col++)
                {
                    var cell = ws.Cell(row, col);
                    cell.Style.Font.FontSize = 11;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.Black;
                }
            }

            // Freeze panes at row 10 (headers stay visible when scrolling)
            ws.SheetView.FreezeRows(10);

            // Print settings: landscape, fit to page width
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PagesWide = 1;
            ws.PageSetup.PagesTall = 0; // auto
            ws.PageSetup.FitToPages(1, 0);

            workbook.SaveAs(filePath);
        }, ct);
    }

    public async Task ExportReportAsync(DateTime startDate, DateTime endDate, string filePath, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var serviceRecords = await context.ServiceRecords
            .Include(sr => sr.Vehicle!)
                .ThenInclude(v => v.Customer)
            .Include(sr => sr.Technician)
            .Where(sr => sr.ServiceDate >= startDate && sr.ServiceDate <= endDate)
            .OrderBy(sr => sr.ServiceDate)
            .ToListAsync(ct);

        var payments = await context.Payments
            .Include(p => p.Customer)
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

        var expenses = await context.DailyExpenses
            .Include(e => e.Category)
            .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .OrderBy(e => e.ExpenseDate)
            .ToListAsync(ct);

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();

            // Sheet 1: Özet (Summary)
            var summarySheet = workbook.Worksheets.Add("Özet");
            var headerBg = XLColor.FromHtml("#002060");

            summarySheet.Cell("A1").Value = "BÜLENT OTO ELEKTRİK - RAPOR";
            summarySheet.Range("A1:D1").Merge();
            summarySheet.Cell("A1").Style.Font.FontSize = 18;
            summarySheet.Cell("A1").Style.Font.Bold = true;
            summarySheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#C00000");

            summarySheet.Cell("A2").Value = $"Dönem: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            summarySheet.Range("A2:D2").Merge();
            summarySheet.Cell("A2").Style.Font.FontSize = 12;

            var totalRevenue = serviceRecords.Sum(sr => sr.TotalAmount);
            var totalPayments = payments.Sum(p => p.Amount);
            var totalExpenses = expenses.Sum(e => e.Amount);

            summarySheet.Cell("A4").Value = "Toplam Gelir (İşlemler)";
            summarySheet.Cell("B4").Value = totalRevenue;
            summarySheet.Cell("B4").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

            summarySheet.Cell("A5").Value = "Toplam Ödeme";
            summarySheet.Cell("B5").Value = totalPayments;
            summarySheet.Cell("B5").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

            summarySheet.Cell("A6").Value = "Toplam Gider";
            summarySheet.Cell("B6").Value = totalExpenses;
            summarySheet.Cell("B6").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";

            summarySheet.Cell("A7").Value = "Net Kazanç";
            summarySheet.Cell("B7").Value = totalPayments - totalExpenses;
            summarySheet.Cell("B7").Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
            summarySheet.Cell("B7").Style.Font.Bold = true;

            for (int r = 4; r <= 7; r++)
            {
                summarySheet.Cell(r, 1).Style.Font.Bold = true;
            }

            summarySheet.Column("A").Width = 30;
            summarySheet.Column("B").Width = 20;

            // Sheet 2: İşlemler (Service Records)
            var srSheet = workbook.Worksheets.Add("İşlemler");
            var srHeaders = new[] { "S.NO", "TARİH", "MÜŞTERİ", "PLAKA", "ŞİKAYET", "YAPILAN İŞLEM", "TEKNİSYEN", "MİKTAR", "BİRİM FİYAT", "TUTAR" };
            for (int col = 1; col <= srHeaders.Length; col++)
            {
                var cell = srSheet.Cell(1, col);
                cell.Value = srHeaders[col - 1];
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < serviceRecords.Count; i++)
            {
                var sr = serviceRecords[i];
                int row = i + 2;
                srSheet.Cell(row, 1).Value = i + 1;
                srSheet.Cell(row, 2).Value = sr.ServiceDate.ToString("dd/MM/yyyy");
                srSheet.Cell(row, 3).Value = sr.Vehicle?.Customer?.FullName ?? "";
                srSheet.Cell(row, 4).Value = sr.Vehicle?.PlateNumber ?? "";
                srSheet.Cell(row, 5).Value = sr.Complaint ?? "";
                srSheet.Cell(row, 6).Value = sr.WorkPerformed;
                srSheet.Cell(row, 7).Value = sr.Technician?.FullName ?? "";
                srSheet.Cell(row, 8).Value = sr.Quantity;
                srSheet.Cell(row, 9).Value = sr.UnitPrice;
                srSheet.Cell(row, 9).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
                srSheet.Cell(row, 10).Value = sr.TotalAmount;
                srSheet.Cell(row, 10).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
            }

            srSheet.Columns().AdjustToContents();

            // Sheet 3: Ödemeler (Payments)
            var paySheet = workbook.Worksheets.Add("Ödemeler");
            var payHeaders = new[] { "S.NO", "TARİH", "MÜŞTERİ", "TUTAR", "ÖDEME YÖNTEMİ", "NOTLAR" };
            for (int col = 1; col <= payHeaders.Length; col++)
            {
                var cell = paySheet.Cell(1, col);
                cell.Value = payHeaders[col - 1];
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < payments.Count; i++)
            {
                var p = payments[i];
                int row = i + 2;
                paySheet.Cell(row, 1).Value = i + 1;
                paySheet.Cell(row, 2).Value = p.PaymentDate.ToString("dd/MM/yyyy");
                paySheet.Cell(row, 3).Value = p.Customer?.FullName ?? "";
                paySheet.Cell(row, 4).Value = p.Amount;
                paySheet.Cell(row, 4).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
                paySheet.Cell(row, 5).Value = p.PaymentMethod.ToString();
                paySheet.Cell(row, 6).Value = p.Notes ?? "";
            }

            paySheet.Columns().AdjustToContents();

            // Sheet 4: Giderler (Expenses)
            var expSheet = workbook.Worksheets.Add("Giderler");
            var expHeaders = new[] { "S.NO", "TARİH", "KATEGORİ", "AÇIKLAMA", "TUTAR" };
            for (int col = 1; col <= expHeaders.Length; col++)
            {
                var cell = expSheet.Cell(1, col);
                cell.Value = expHeaders[col - 1];
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < expenses.Count; i++)
            {
                var e = expenses[i];
                int row = i + 2;
                expSheet.Cell(row, 1).Value = i + 1;
                expSheet.Cell(row, 2).Value = e.ExpenseDate.ToString("dd/MM/yyyy");
                expSheet.Cell(row, 3).Value = e.Category?.Name ?? "";
                expSheet.Cell(row, 4).Value = e.Description ?? "";
                expSheet.Cell(row, 5).Value = e.Amount;
                expSheet.Cell(row, 5).Style.NumberFormat.Format = @"#,##0.00\ ""TL""";
            }

            expSheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }, ct);
    }

    public async Task AutoExportCustomerCardsAsync(int customerId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId, ct);

            if (customer == null) return;

            var vehicles = await context.Vehicles
                .AsNoTracking()
                .Where(v => v.CustomerId == customerId)
                .ToListAsync(ct);

            Directory.CreateDirectory(_exportFolder);

            foreach (var vehicle in vehicles)
            {
                try
                {
                    var safeName = string.Join("_", $"{customer.FullName}_{vehicle.PlateNumber}".Split(Path.GetInvalidFileNameChars()));
                    var filePath = Path.Combine(_exportFolder, $"{safeName}.xlsx");
                    await ExportCustomerCardAsync(customerId, vehicle.Id, filePath, ct);
                }
                catch
                {
                    // Continue exporting remaining vehicles even if one fails
                }
            }
        }
        catch
        {
            // Silently handle errors since this runs as fire-and-forget background task
        }
    }

    public async Task AutoExportReportsAsync(DateTime date, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_exportFolder);
            var d = date.Date;

            // Daily: just that day
            var dailyStart = d;
            var dailyEnd = d;
            var dailyFile = Path.Combine(_exportFolder, $"Rapor_Gunluk_{d:yyyyMMdd}.xlsx");

            // Weekly: Monday to Sunday
            var dayOfWeek = (int)d.DayOfWeek;
            var monday = d.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
            var sunday = monday.AddDays(6);
            var weeklyFile = Path.Combine(_exportFolder, $"Rapor_Haftalik_{monday:yyyyMMdd}_{sunday:yyyyMMdd}.xlsx");

            // Monthly: first to last day
            var monthStart = new DateTime(d.Year, d.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthlyFile = Path.Combine(_exportFolder, $"Rapor_Aylik_{d:yyyyMM}.xlsx");

            // Yearly: Jan 1 to Dec 31
            var yearStart = new DateTime(d.Year, 1, 1);
            var yearEnd = new DateTime(d.Year, 12, 31);
            var yearlyFile = Path.Combine(_exportFolder, $"Rapor_Yillik_{d.Year}.xlsx");

            var exports = new (DateTime start, DateTime end, string path)[]
            {
                (dailyStart, dailyEnd, dailyFile),
                (monday, sunday, weeklyFile),
                (monthStart, monthEnd, monthlyFile),
                (yearStart, yearEnd, yearlyFile)
            };

            foreach (var (start, end, path) in exports)
            {
                try
                {
                    await ExportReportAsync(start, end, path, ct);
                }
                catch
                {
                    // Continue exporting remaining periods even if one fails
                }
            }
        }
        catch
        {
            // Silently handle errors since this runs as fire-and-forget background task
        }
    }

    public async Task AutoExportAllAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Directory.CreateDirectory(_exportFolder);

            // Export all customer cards
            var customers = await context.Customers
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var customer in customers)
            {
                var vehicles = await context.Vehicles
                    .AsNoTracking()
                    .Where(v => v.CustomerId == customer.Id)
                    .ToListAsync(ct);

                foreach (var vehicle in vehicles)
                {
                    try
                    {
                        var safeName = string.Join("_", $"{customer.FullName}_{vehicle.PlateNumber}".Split(Path.GetInvalidFileNameChars()));
                        var filePath = Path.Combine(_exportFolder, $"{safeName}.xlsx");
                        await ExportCustomerCardAsync(customer.Id, vehicle.Id, filePath, ct);
                    }
                    catch { }
                }
            }

            // Find all distinct dates that have data and export reports for each
            var serviceDates = await context.ServiceRecords
                .AsNoTracking()
                .Select(sr => sr.ServiceDate.Date)
                .Distinct()
                .ToListAsync(ct);

            var paymentDates = await context.Payments
                .AsNoTracking()
                .Select(p => p.PaymentDate.Date)
                .Distinct()
                .ToListAsync(ct);

            var expenseDates = await context.DailyExpenses
                .AsNoTracking()
                .Select(e => e.ExpenseDate.Date)
                .Distinct()
                .ToListAsync(ct);

            var allDates = serviceDates
                .Union(paymentDates)
                .Union(expenseDates)
                .Distinct()
                .ToList();

            // Track which periods we've already exported to avoid duplicates
            var exportedWeeks = new HashSet<string>();
            var exportedMonths = new HashSet<string>();
            var exportedYears = new HashSet<int>();

            foreach (var date in allDates)
            {
                try
                {
                    // Daily
                    var dailyFile = Path.Combine(_exportFolder, $"Rapor_Gunluk_{date:yyyyMMdd}.xlsx");
                    await ExportReportAsync(date, date, dailyFile, ct);

                    // Weekly (only once per week)
                    var dow = (int)date.DayOfWeek;
                    var monday = date.AddDays(-(dow == 0 ? 6 : dow - 1));
                    var sunday = monday.AddDays(6);
                    var weekKey = $"{monday:yyyyMMdd}";
                    if (exportedWeeks.Add(weekKey))
                    {
                        var weeklyFile = Path.Combine(_exportFolder, $"Rapor_Haftalik_{monday:yyyyMMdd}_{sunday:yyyyMMdd}.xlsx");
                        await ExportReportAsync(monday, sunday, weeklyFile, ct);
                    }

                    // Monthly (only once per month)
                    var monthKey = $"{date:yyyyMM}";
                    if (exportedMonths.Add(monthKey))
                    {
                        var monthStart = new DateTime(date.Year, date.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        var monthlyFile = Path.Combine(_exportFolder, $"Rapor_Aylik_{date:yyyyMM}.xlsx");
                        await ExportReportAsync(monthStart, monthEnd, monthlyFile, ct);
                    }

                    // Yearly (only once per year)
                    if (exportedYears.Add(date.Year))
                    {
                        var yearStart = new DateTime(date.Year, 1, 1);
                        var yearEnd = new DateTime(date.Year, 12, 31);
                        var yearlyFile = Path.Combine(_exportFolder, $"Rapor_Yillik_{date.Year}.xlsx");
                        await ExportReportAsync(yearStart, yearEnd, yearlyFile, ct);
                    }
                }
                catch { }
            }
        }
        catch
        {
            // Silently handle errors since this runs as fire-and-forget background task
        }
    }

    private static void SetLabelRow(IXLWorksheet ws, int row, string label, string value, bool isRedValue)
    {
        // Merge A:B for label
        ws.Range(row, 1, row, 2).Merge();
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFFCC");
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Merge C:H for value
        ws.Range(row, 3, row, 8).Merge();
        ws.Cell(row, 3).Value = value;
        ws.Cell(row, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        if (isRedValue)
        {
            ws.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#FF0000");
        }
    }
}
