using System.ComponentModel.DataAnnotations.Schema;

namespace BulentOtoElektrik.Core.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // Computed (not mapped to DB)
    [NotMapped]
    public decimal TotalDebt => Vehicles?.SelectMany(v => v.ServiceRecords ?? Enumerable.Empty<ServiceRecord>()).Sum(sr => sr.TotalAmount) ?? 0;
    [NotMapped]
    public decimal TotalPayments => Payments?.Sum(p => p.Amount) ?? 0;
    [NotMapped]
    public decimal Balance => TotalDebt - TotalPayments;
}
