using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IServiceRecordRepository
{
    Task<List<ServiceRecord>> GetByVehicleIdAsync(int vehicleId, CancellationToken ct = default);
    Task<List<ServiceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<List<ServiceRecord>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<int> GetTodayVehicleCountAsync(CancellationToken ct = default);
    Task<ServiceRecord> AddAsync(ServiceRecord record, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ServiceRecord> records, CancellationToken ct = default);
    Task UpdateAsync(ServiceRecord record, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
