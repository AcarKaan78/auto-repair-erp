using BulentOtoElektrik.Core.Enums;

namespace BulentOtoElektrik.Core.Entities;

public class Payment : BaseEntity
{
    public int CustomerId { get; set; }
    public int? VehicleId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.TL;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
}
