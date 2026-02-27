namespace BulentOtoElektrik.Core.DTOs;

public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenses;
    public int VehicleCount { get; set; }
    public double RevenueChangePercent { get; set; }
    public double ExpenseChangePercent { get; set; }
    public double NetChangePercent { get; set; }
}
