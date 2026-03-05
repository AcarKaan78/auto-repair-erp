using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly AppDbContext _context;

    public ExpenseCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseCategory>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ExpenseCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<List<ExpenseCategory>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _context.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<ExpenseCategory> AddAsync(ExpenseCategory category, CancellationToken ct = default)
    {
        _context.ExpenseCategories.Add(category);
        return category;
    }

    public async Task UpdateAsync(ExpenseCategory category, CancellationToken ct = default)
    {
        _context.ExpenseCategories.Update(category);
    }
}
