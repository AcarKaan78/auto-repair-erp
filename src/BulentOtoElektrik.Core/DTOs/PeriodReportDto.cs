namespace BulentOtoElektrik.Core.DTOs;

public class PeriodReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetEarnings => TotalRevenue - TotalExpenses;
    public List<DailyBreakdownDto> DailyBreakdown { get; set; } = new();
}
