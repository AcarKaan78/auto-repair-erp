using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly AppDbContext _context;

    public StockItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.StockItems
            .AsNoTracking()
            .OrderBy(s => s.MaterialName)
            .ToListAsync(ct);
    }

    public async Task<List<StockItem>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _context.StockItems
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.MaterialName)
            .ToListAsync(ct);
    }

    public async Task<StockItem> AddAsync(StockItem stockItem, CancellationToken ct = default)
    {
        _context.StockItems.Add(stockItem);
        await _context.SaveChangesAsync(ct);
        return stockItem;
    }

    public async Task UpdateAsync(StockItem stockItem, CancellationToken ct = default)
    {
        var tracked = _context.ChangeTracker.Entries<StockItem>()
            .FirstOrDefault(e => e.Entity.Id == stockItem.Id);

        if (tracked != null)
        {
            tracked.CurrentValues.SetValues(stockItem);
        }
        else
        {
            _context.StockItems.Update(stockItem);
        }

        await _context.SaveChangesAsync(ct);
    }
}
