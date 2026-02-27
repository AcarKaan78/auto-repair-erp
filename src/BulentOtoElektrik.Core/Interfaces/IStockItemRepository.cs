using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IStockItemRepository
{
    Task<List<StockItem>> GetAllAsync(CancellationToken ct = default);
    Task<List<StockItem>> GetActiveAsync(CancellationToken ct = default);
    Task<StockItem> AddAsync(StockItem stockItem, CancellationToken ct = default);
    Task UpdateAsync(StockItem stockItem, CancellationToken ct = default);
}
