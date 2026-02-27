using BulentOtoElektrik.Core.Enums;

namespace BulentOtoElektrik.Core.Entities;

public class ServiceRecord : BaseEntity
{
    public int VehicleId { get; set; }
    public int? TechnicianId { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public string? Complaint { get; set; }
    public string WorkPerformed { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; } // Stored, computed as Quantity * UnitPrice in SaveChanges
    public CurrencyType Currency { get; set; } = CurrencyType.TL;
    public string? Notes { get; set; }
    public int? StockItemId { get; set; }
    public int MaterialQuantityUsed { get; set; }

    // Navigation
    public Vehicle Vehicle { get; set; } = null!;
    public Technician? Technician { get; set; }
    public StockItem? StockItem { get; set; }
}
