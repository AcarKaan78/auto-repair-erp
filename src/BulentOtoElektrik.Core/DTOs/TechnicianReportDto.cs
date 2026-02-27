namespace BulentOtoElektrik.Core.DTOs;

public class TechnicianReportDto
{
    public string TechnicianName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int ServiceCount { get; set; }
}
