using BulentOtoElektrik.Core.DTOs;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IReportingService
{
    Task<DailySummaryDto> GetDailySummaryAsync(DateTime date, CancellationToken ct = default);
    Task<PeriodReportDto> GetPeriodReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<TechnicianReportDto>> GetTechnicianReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<ExpenseBreakdownDto>> GetExpenseBreakdownAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
