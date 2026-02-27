namespace BulentOtoElektrik.Core.Entities;

public class Vehicle : BaseEntity
{
    public int CustomerId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleBrand { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
