namespace BulentOtoElektrik.Core.DTOs;

public class VehicleSearchResult
{
    public int VehicleId { get; set; }
    public int CustomerId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
