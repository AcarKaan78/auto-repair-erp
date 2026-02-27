using System.ComponentModel.DataAnnotations.Schema;

namespace BulentOtoElektrik.Core.Entities;

public class StockItem : BaseEntity
{
    public string MaterialName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();

    [NotMapped]
    public decimal TotalValue => RemainingQuantity * UnitPrice;

    [NotMapped]
    public string DisplayText => $"{MaterialName} - {UnitPrice:N2} ₺";
}
