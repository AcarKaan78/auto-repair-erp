using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IExpenseCategoryRepository
{
    Task<List<ExpenseCategory>> GetAllAsync(CancellationToken ct = default);
    Task<List<ExpenseCategory>> GetActiveAsync(CancellationToken ct = default);
    Task<ExpenseCategory> AddAsync(ExpenseCategory category, CancellationToken ct = default);
    Task UpdateAsync(ExpenseCategory category, CancellationToken ct = default);
}
