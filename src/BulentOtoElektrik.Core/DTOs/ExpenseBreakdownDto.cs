namespace BulentOtoElektrik.Core.DTOs;

public class ExpenseBreakdownDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}
