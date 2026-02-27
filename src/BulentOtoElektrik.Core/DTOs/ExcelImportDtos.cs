using BulentOtoElektrik.Core.Enums;

namespace BulentOtoElektrik.Core.DTOs;

public enum ExcelFormatType { OldFormat, AppFormat, Unknown }

public class ExcelImportResultDto
{
    public int TotalFiles { get; set; }
    public int SuccessfulFiles { get; set; }
    public int FailedFiles { get; set; }
    public int CustomersCreated { get; set; }
    public int VehiclesCreated { get; set; }
    public int ServiceRecordsCreated { get; set; }
    public int PaymentsCreated { get; set; }
    public int TechniciansCreated { get; set; }
    public int VehiclesSkipped { get; set; }
    public int VehiclesMerged { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ExcelFileParseResultDto
{
    public string FilePath { get; set; } = string.Empty;
    public ExcelFormatType Format { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Customer metadata
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Vehicle metadata
    public string PlateNumber { get; set; } = string.Empty;
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.TL;

    // Parsed rows
    public List<ParsedServiceRow> ServiceRows { get; set; } = new();
    public List<ParsedPaymentRow> PaymentRows { get; set; } = new();
}

public class ParsedServiceRow
{
    public DateTime ServiceDate { get; set; }
    public string? Complaint { get; set; }
    public string WorkPerformed { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

public class ParsedPaymentRow
{
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
}
