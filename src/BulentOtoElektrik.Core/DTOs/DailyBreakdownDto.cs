namespace BulentOtoElektrik.Core.DTOs;

public class DailyBreakdownDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net => Revenue - Expenses;
}
