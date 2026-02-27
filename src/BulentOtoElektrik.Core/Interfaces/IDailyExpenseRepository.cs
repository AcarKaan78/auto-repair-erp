using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IDailyExpenseRepository
{
    Task<List<DailyExpense>> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<List<DailyExpense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<DailyExpense> AddAsync(DailyExpense expense, CancellationToken ct = default);
    Task UpdateAsync(DailyExpense expense, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
