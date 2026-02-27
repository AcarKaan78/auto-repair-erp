namespace BulentOtoElektrik.Core.Entities;

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<DailyExpense> Expenses { get; set; } = new List<DailyExpense>();
}
