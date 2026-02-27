using BulentOtoElektrik.Core.Enums;

namespace BulentOtoElektrik.Core.Entities;

public class DailyExpense : BaseEntity
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public CurrencyType Currency { get; set; } = CurrencyType.TL;
    public string? Notes { get; set; }

    // Navigation
    public ExpenseCategory Category { get; set; } = null!;
}
