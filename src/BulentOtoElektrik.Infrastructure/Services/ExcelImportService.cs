using System.Globalization;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public ExcelImportService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<ExcelFileParseResultDto> ParseFileAsync(string filePath, CancellationToken ct = default)
    {
        return Task.Run(() => ParseFile(filePath), ct);
    }

    public async Task<ExcelImportResultDto> ImportFilesAsync(
        IReadOnlyList<string> filePaths,
        string duplicateAction,
        Action<int, int>? progressCallback = null,
        CancellationToken ct = default)
    {
        var result = new ExcelImportResultDto { TotalFiles = filePaths.Count };

        for (int i = 0; i < filePaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            progressCallback?.Invoke(i + 1, filePaths.Count);

            var filePath = filePaths[i];
            var fileName = Path.GetFileName(filePath);

            try
            {
                var parsed = ParseFile(filePath);
                if (!parsed.Success)
                {
                    result.FailedFiles++;
                    result.Errors.Add($"{fileName}: {parsed.ErrorMessage}");
                    continue;
                }

                var persistResult = await PersistAsync(parsed, duplicateAction, ct);
                result.SuccessfulFiles++;
                result.CustomersCreated += persistResult.CustomersCreated;
                result.VehiclesCreated += persistResult.VehiclesCreated;
                result.ServiceRecordsCreated += persistResult.ServiceRecordsCreated;
                result.PaymentsCreated += persistResult.PaymentsCreated;
                result.TechniciansCreated += persistResult.TechniciansCreated;
                result.VehiclesSkipped += persistResult.VehiclesSkipped;
                result.VehiclesMerged += persistResult.VehiclesMerged;
                result.Warnings.AddRange(persistResult.Warnings);
            }
            catch (Exception ex)
            {
                result.FailedFiles++;
                result.Errors.Add($"{fileName}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ExcelImportResultDto> ImportFolderAsync(
        string folderPath,
        string duplicateAction,
        Action<int, int>? progressCallback = null,
        CancellationToken ct = default)
    {
        var files = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).StartsWith("~$")) // skip temp files
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            return new ExcelImportResultDto
            {
                Errors = { "Seçilen klasörde .xlsx dosyası bulunamadı." }
            };
        }

        return await ImportFilesAsync(files, duplicateAction, progressCallback, ct);
    }

    private ExcelFileParseResultDto ParseFile(string filePath)
    {
        var result = new ExcelFileParseResultDto { FilePath = filePath };

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(1);

            result.Format = DetectFormat(ws);
            if (result.Format == ExcelFormatType.Unknown)
            {
                result.Success = false;
                result.ErrorMessage = "Tanınmayan Excel formatı. Eski format veya uygulama formatı bekleniyor.";
                return result;
            }

            if (result.Format == ExcelFormatType.OldFormat)
                ParseOldFormat(ws, result);
            else
                ParseAppFormat(ws, result);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(result.CustomerName))
            {
                result.Success = false;
                result.ErrorMessage = "Müşteri adı bulunamadı.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(result.PlateNumber))
            {
                result.Success = false;
                result.ErrorMessage = "Plaka numarası bulunamadı.";
                return result;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Dosya okunamadı: {ex.Message}";
        }

        return result;
    }

    private static ExcelFormatType DetectFormat(IXLWorksheet ws)
    {
        var cellA10 = GetCellString(ws, 10, 1);

        if (cellA10.Contains("SIRA", StringComparison.OrdinalIgnoreCase))
            return ExcelFormatType.OldFormat;

        if (cellA10.Contains("S.NO", StringComparison.OrdinalIgnoreCase))
            return ExcelFormatType.AppFormat;

        // Fallback: check row 2 label
        var cellA2 = GetCellString(ws, 2, 1);
        if (cellA2.Contains("PLAKA", StringComparison.OrdinalIgnoreCase))
            return ExcelFormatType.OldFormat;
        if (cellA2.Contains("TELEFON", StringComparison.OrdinalIgnoreCase))
            return ExcelFormatType.AppFormat;

        return ExcelFormatType.Unknown;
    }

    private static void ParseOldFormat(IXLWorksheet ws, ExcelFileParseResultDto result)
    {
        // Read header metadata by scanning rows 1-8 for labels
        // Use OrdinalIgnoreCase to avoid Turkish İ/I culture issues
        for (int row = 1; row <= 8; row++)
        {
            var label = GetCellString(ws, row, 1).Trim();
            var value = GetCellString(ws, row, 3).Trim();

            if (ContainsAny(label, "ADI", "SOYAD"))
                result.CustomerName = value;
            else if (ContainsAny(label, "PLAKA"))
                result.PlateNumber = NormalizePlate(value);
            else if (ContainsAny(label, "ARAC", "MODEL"))
                SplitBrandModel(value, result);
            else if (ContainsAny(label, "CEP 1", "CEP1") || (ContainsAny(label, "CEP") && !ContainsAny(label, "2")))
                result.Phone1 = string.IsNullOrWhiteSpace(value) ? null : value;
            else if (ContainsAny(label, "CEP 2", "CEP2"))
                result.Phone2 = string.IsNullOrWhiteSpace(value) ? null : value;
            else if (ContainsAny(label, "KIMLIK", "KİMLİK"))
                result.IdentityNumber = string.IsNullOrWhiteSpace(value) ? null : value;
            else if (ContainsAny(label, "MAIL"))
                result.Email = string.IsNullOrWhiteSpace(value) ? null : value;
            else if (ContainsAny(label, "ADRES"))
                result.Address = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // Read currency from I1/J1
        result.Currency = ParseCurrency(GetCellString(ws, 1, 10)); // J1

        // Parse data rows starting from row 11
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 10;
        for (int row = 11; row <= lastRow; row++)
        {
            var dateValue = TryParseDate(ws.Cell(row, 2)); // Col B = date
            if (dateValue == null)
                continue; // skip empty/template rows

            var workPerformed = GetCellString(ws, row, 4).Trim(); // Col D
            if (string.IsNullOrWhiteSpace(workPerformed))
                continue; // skip rows with no work description

            var complaint = GetCellString(ws, row, 3).Trim();
            var technicianName = GetCellString(ws, row, 5).Trim();
            var quantity = GetCellInt(ws, row, 6);
            var unitPrice = GetCellDecimal(ws, row, 7);
            var totalAmount = GetCellDecimal(ws, row, 8);

            // Normalize: if quantity is 0 but totalAmount > 0, set qty=1, unitPrice=totalAmount
            if (quantity <= 0 && totalAmount > 0)
            {
                quantity = 1;
                unitPrice = totalAmount;
            }
            else if (quantity <= 0)
            {
                quantity = 1;
            }

            // If unitPrice is 0 but totalAmount > 0, derive it
            if (unitPrice == 0 && totalAmount > 0 && quantity > 0)
            {
                unitPrice = totalAmount / quantity;
            }

            result.ServiceRows.Add(new ParsedServiceRow
            {
                ServiceDate = dateValue.Value,
                Complaint = string.IsNullOrWhiteSpace(complaint) ? null : complaint,
                WorkPerformed = workPerformed,
                TechnicianName = string.IsNullOrWhiteSpace(technicianName) ? null : technicianName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalAmount = totalAmount
            });

            // Col I = ODEME (payment) in OLD format
            var paymentAmount = GetCellDecimal(ws, row, 9);
            if (paymentAmount > 0)
            {
                result.PaymentRows.Add(new ParsedPaymentRow
                {
                    PaymentDate = dateValue.Value,
                    Amount = paymentAmount
                });
            }
        }
    }

    private static void ParseAppFormat(IXLWorksheet ws, ExcelFileParseResultDto result)
    {
        // APP format: fixed row positions
        // Row 1: AD-SOYAD, Row 2: TELEFON 1, Row 3: TELEFON 2, Row 4: T.C NO
        // Row 5: PLAKA, Row 6: MARKA, Row 7: MODEL, Row 8: YIL
        result.CustomerName = GetCellString(ws, 1, 3).Trim();
        result.Phone1 = NullIfEmpty(GetCellString(ws, 2, 3).Trim());
        result.Phone2 = NullIfEmpty(GetCellString(ws, 3, 3).Trim());
        result.IdentityNumber = NullIfEmpty(GetCellString(ws, 4, 3).Trim());
        result.PlateNumber = NormalizePlate(GetCellString(ws, 5, 3).Trim());
        result.VehicleBrand = NullIfEmpty(GetCellString(ws, 6, 3).Trim());
        result.VehicleModel = NullIfEmpty(GetCellString(ws, 7, 3).Trim());

        var yearStr = GetCellString(ws, 8, 3).Trim();
        if (int.TryParse(yearStr, out var year) && year > 1900 && year < 2100)
            result.VehicleYear = year;

        // Currency from J1
        result.Currency = ParseCurrency(GetCellString(ws, 1, 10));

        // Parse data rows from row 11
        // APP format cols: S.NO | TARIH | SIKAYET | YAPILAN ISLEM | TEKNISYEN | MIKTAR | BIRIM FIYAT | TUTAR | KALAN BAKIYE | NOTLAR
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 10;
        for (int row = 11; row <= lastRow; row++)
        {
            var dateValue = TryParseDateString(GetCellString(ws, row, 2)); // Col B = date string
            if (dateValue == null)
            {
                // Also try as datetime
                dateValue = TryParseDate(ws.Cell(row, 2));
                if (dateValue == null)
                    continue;
            }

            var workPerformed = GetCellString(ws, row, 4).Trim();
            if (string.IsNullOrWhiteSpace(workPerformed))
                continue;

            var complaint = GetCellString(ws, row, 3).Trim();
            var technicianName = GetCellString(ws, row, 5).Trim();
            var quantity = GetCellInt(ws, row, 6);
            var unitPrice = GetCellDecimal(ws, row, 7);
            var totalAmount = GetCellDecimal(ws, row, 8);
            var notes = GetCellString(ws, row, 10).Trim();

            if (quantity <= 0 && totalAmount > 0)
            {
                quantity = 1;
                unitPrice = totalAmount;
            }
            else if (quantity <= 0)
            {
                quantity = 1;
            }

            if (unitPrice == 0 && totalAmount > 0 && quantity > 0)
            {
                unitPrice = totalAmount / quantity;
            }

            result.ServiceRows.Add(new ParsedServiceRow
            {
                ServiceDate = dateValue.Value,
                Complaint = string.IsNullOrWhiteSpace(complaint) ? null : complaint,
                WorkPerformed = workPerformed,
                TechnicianName = string.IsNullOrWhiteSpace(technicianName) ? null : technicianName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalAmount = totalAmount,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
            });

            // APP format has no payment column — payments are tracked separately
        }
    }

    private async Task<ExcelImportResultDto> PersistAsync(
        ExcelFileParseResultDto parsed,
        string duplicateAction,
        CancellationToken ct)
    {
        var result = new ExcelImportResultDto();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var normalizedPlate = NormalizePlate(parsed.PlateNumber);

        // Check for existing vehicle by plate
        var existingVehicle = await context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceRecords)
            .FirstOrDefaultAsync(v => v.PlateNumber == normalizedPlate, ct);

        if (existingVehicle != null)
        {
            if (duplicateAction == "skip")
            {
                result.VehiclesSkipped++;
                result.Warnings.Add($"{parsed.PlateNumber}: Araç zaten mevcut, atlandı.");
                return result;
            }

            // merge: add records to existing vehicle
            result.VehiclesMerged++;

            var customer = existingVehicle.Customer;

            // Update customer info if fields were empty
            UpdateCustomerIfEmpty(customer, parsed);
            context.Customers.Update(customer);

            // Resolve technicians and add service records
            var technicianMap = await GetOrCreateTechniciansAsync(context, parsed.ServiceRows, result, ct);

            var existingRecordKeys = existingVehicle.ServiceRecords?
                .Select(sr => (sr.ServiceDate.Date, sr.WorkPerformed.ToUpper(TrCulture)))
                .ToHashSet() ?? new();

            foreach (var row in parsed.ServiceRows)
            {
                var key = (row.ServiceDate.Date, row.WorkPerformed.ToUpper(TrCulture));
                if (existingRecordKeys.Contains(key))
                    continue; // skip duplicate service record

                var sr = new ServiceRecord
                {
                    VehicleId = existingVehicle.Id,
                    ServiceDate = row.ServiceDate,
                    Complaint = row.Complaint,
                    WorkPerformed = row.WorkPerformed,
                    Quantity = row.Quantity,
                    UnitPrice = row.UnitPrice,
                    Currency = parsed.Currency
                };

                if (row.TechnicianName != null && technicianMap.TryGetValue(row.TechnicianName.ToUpper(TrCulture), out var techId))
                    sr.TechnicianId = techId;

                context.ServiceRecords.Add(sr);
                result.ServiceRecordsCreated++;
            }

            // Add payments
            foreach (var payRow in parsed.PaymentRows)
            {
                context.Payments.Add(new Payment
                {
                    CustomerId = customer.Id,
                    VehicleId = existingVehicle.Id,
                    PaymentDate = payRow.PaymentDate,
                    Amount = payRow.Amount,
                    Currency = parsed.Currency,
                    PaymentMethod = PaymentMethod.Cash,
                    Notes = "Excel'den içe aktarıldı"
                });
                result.PaymentsCreated++;
            }

            await context.SaveChangesAsync(ct);
            return result;
        }

        // No existing vehicle — find or create customer
        var normalizedName = parsed.CustomerName.ToUpper(TrCulture);
        var existingCustomer = await context.Customers
            .FirstOrDefaultAsync(c => c.FullName.ToUpper() == normalizedName, ct);

        Customer targetCustomer;
        if (existingCustomer != null)
        {
            targetCustomer = existingCustomer;
            UpdateCustomerIfEmpty(targetCustomer, parsed);
            context.Customers.Update(targetCustomer);
        }
        else
        {
            targetCustomer = new Customer
            {
                FullName = parsed.CustomerName,
                Phone1 = parsed.Phone1,
                Phone2 = parsed.Phone2,
                IdentityNumber = parsed.IdentityNumber,
                Email = parsed.Email,
                Address = parsed.Address
            };
            context.Customers.Add(targetCustomer);
            await context.SaveChangesAsync(ct); // get customer ID
            result.CustomersCreated++;
        }

        // Create vehicle
        var vehicle = new Vehicle
        {
            CustomerId = targetCustomer.Id,
            PlateNumber = normalizedPlate,
            VehicleBrand = parsed.VehicleBrand,
            VehicleModel = parsed.VehicleModel,
            VehicleYear = parsed.VehicleYear
        };
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(ct); // get vehicle ID
        result.VehiclesCreated++;

        // Resolve technicians
        var techMap = await GetOrCreateTechniciansAsync(context, parsed.ServiceRows, result, ct);

        // Add service records
        foreach (var row in parsed.ServiceRows)
        {
            var sr = new ServiceRecord
            {
                VehicleId = vehicle.Id,
                ServiceDate = row.ServiceDate,
                Complaint = row.Complaint,
                WorkPerformed = row.WorkPerformed,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                Currency = parsed.Currency
            };

            if (row.TechnicianName != null && techMap.TryGetValue(row.TechnicianName.ToUpper(TrCulture), out var techId))
                sr.TechnicianId = techId;

            context.ServiceRecords.Add(sr);
            result.ServiceRecordsCreated++;
        }

        // Add payments
        foreach (var payRow in parsed.PaymentRows)
        {
            context.Payments.Add(new Payment
            {
                CustomerId = targetCustomer.Id,
                VehicleId = vehicle.Id,
                PaymentDate = payRow.PaymentDate,
                Amount = payRow.Amount,
                Currency = parsed.Currency,
                PaymentMethod = PaymentMethod.Cash,
                Notes = "Excel'den içe aktarıldı"
            });
            result.PaymentsCreated++;
        }

        await context.SaveChangesAsync(ct);
        return result;
    }

    private static async Task<Dictionary<string, int>> GetOrCreateTechniciansAsync(
        AppDbContext context,
        List<ParsedServiceRow> serviceRows,
        ExcelImportResultDto result,
        CancellationToken ct)
    {
        var techMap = new Dictionary<string, int>();
        var existingTechnicians = await context.Technicians.ToListAsync(ct);

        var uniqueNames = serviceRows
            .Where(r => !string.IsNullOrWhiteSpace(r.TechnicianName))
            .Select(r => r.TechnicianName!)
            .Distinct()
            .ToList();

        foreach (var name in uniqueNames)
        {
            var normalized = name.ToUpper(TrCulture);
            var existing = existingTechnicians.FirstOrDefault(
                t => t.FullName.ToUpper(TrCulture) == normalized);

            if (existing != null)
            {
                techMap[normalized] = existing.Id;
            }
            else
            {
                var newTech = new Technician { FullName = name, IsActive = true };
                context.Technicians.Add(newTech);
                await context.SaveChangesAsync(ct);
                techMap[normalized] = newTech.Id;
                existingTechnicians.Add(newTech);
                result.TechniciansCreated++;
            }
        }

        return techMap;
    }

    private static void UpdateCustomerIfEmpty(Customer customer, ExcelFileParseResultDto parsed)
    {
        if (string.IsNullOrWhiteSpace(customer.Phone1) && !string.IsNullOrWhiteSpace(parsed.Phone1))
            customer.Phone1 = parsed.Phone1;
        if (string.IsNullOrWhiteSpace(customer.Phone2) && !string.IsNullOrWhiteSpace(parsed.Phone2))
            customer.Phone2 = parsed.Phone2;
        if (string.IsNullOrWhiteSpace(customer.IdentityNumber) && !string.IsNullOrWhiteSpace(parsed.IdentityNumber))
            customer.IdentityNumber = parsed.IdentityNumber;
        if (string.IsNullOrWhiteSpace(customer.Email) && !string.IsNullOrWhiteSpace(parsed.Email))
            customer.Email = parsed.Email;
        if (string.IsNullOrWhiteSpace(customer.Address) && !string.IsNullOrWhiteSpace(parsed.Address))
            customer.Address = parsed.Address;
    }

    #region Helper Methods

    private static string GetCellString(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsMerged())
        {
            var mergedRange = cell.MergedRange();
            cell = mergedRange.FirstCell();
        }
        return cell.GetString() ?? string.Empty;
    }

    private static int GetCellInt(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        try
        {
            if (cell.DataType == XLDataType.Number)
                return (int)cell.GetDouble();
            var str = cell.GetString();
            if (int.TryParse(str, NumberStyles.Any, TrCulture, out var val))
                return val;
        }
        catch { }
        return 0;
    }

    private static decimal GetCellDecimal(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        try
        {
            if (cell.DataType == XLDataType.Number)
                return (decimal)cell.GetDouble();
            var str = cell.GetString()
                .Replace("TL", "")
                .Replace("USD", "")
                .Replace("EURO", "")
                .Replace("€", "")
                .Replace("$", "")
                .Replace("₺", "")
                .Trim();
            if (decimal.TryParse(str, NumberStyles.Any, TrCulture, out var val))
                return val;
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                return val;
        }
        catch { }
        return 0m;
    }

    private static DateTime? TryParseDate(IXLCell cell)
    {
        try
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();
            if (cell.DataType == XLDataType.Number)
            {
                var num = cell.GetDouble();
                if (num > 1 && num < 200000) // Excel serial date range
                    return DateTime.FromOADate(num);
            }
            // Try parsing as string
            return TryParseDateString(cell.GetString());
        }
        catch { }
        return null;
    }

    private static DateTime? TryParseDateString(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return null;

        str = str.Trim();
        string[] formats = {
            "dd/MM/yyyy", "dd.MM.yyyy", "dd-MM-yyyy",
            "d/M/yyyy", "d.M.yyyy", "d-M-yyyy",
            "MM/dd/yyyy", "M/d/yyyy",
            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(str, formats, TrCulture, DateTimeStyles.None, out var date))
            return date;
        if (DateTime.TryParse(str, TrCulture, DateTimeStyles.None, out date))
            return date;
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;

        return null;
    }

    private static CurrencyType ParseCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return CurrencyType.TL;

        var upper = value.Trim().ToUpper(TrCulture);
        return upper switch
        {
            "USD" or "$" => CurrencyType.USD,
            "EURO" or "EUR" or "€" => CurrencyType.EURO,
            _ => CurrencyType.TL
        };
    }

    private static void SplitBrandModel(string combined, ExcelFileParseResultDto result)
    {
        if (string.IsNullOrWhiteSpace(combined))
            return;

        var parts = combined.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            result.VehicleBrand = parts[0];
            result.VehicleModel = parts[1];
        }
        else if (parts.Length == 1)
        {
            result.VehicleBrand = parts[0];
        }
    }

    private static string NormalizePlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return string.Empty;
        // Keep the plate as-is but trim whitespace
        return plate.Trim().ToUpper(TrCulture);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    #endregion
}
