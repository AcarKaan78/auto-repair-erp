using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class DailyExpenseRepository : IDailyExpenseRepository
{
    private readonly AppDbContext _context;

    public DailyExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DailyExpense>> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        return await _context.DailyExpenses
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.ExpenseDate == date.Date)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<List<DailyExpense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _context.DailyExpenses
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.ExpenseDate >= startDate.Date && e.ExpenseDate <= endDate.Date)
            .OrderBy(e => e.ExpenseDate)
            .ThenBy(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task<DailyExpense> AddAsync(DailyExpense expense, CancellationToken ct = default)
    {
        _context.DailyExpenses.Add(expense);
        await _context.SaveChangesAsync(ct);
        return expense;
    }

    public async Task UpdateAsync(DailyExpense expense, CancellationToken ct = default)
    {
        _context.DailyExpenses.Update(expense);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var expense = await _context.DailyExpenses.FindAsync(new object[] { id }, ct);
        if (expense != null)
        {
            _context.DailyExpenses.Remove(expense);
            await _context.SaveChangesAsync(ct);
        }
    }
}
