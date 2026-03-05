using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class ServiceRecordRepository : IServiceRecordRepository
{
    private readonly AppDbContext _context;

    public ServiceRecordRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceRecord>> GetByVehicleIdAsync(int vehicleId, CancellationToken ct = default)
    {
        return await _context.ServiceRecords
            .AsNoTracking()
            .Include(sr => sr.Technician)
            .Where(sr => sr.VehicleId == vehicleId)
            .OrderBy(sr => sr.ServiceDate)
            .ThenBy(sr => sr.Id)
            .ToListAsync(ct);
    }

    public async Task<List<ServiceRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _context.ServiceRecords
            .AsNoTracking()
            .Include(sr => sr.Technician)
            .Include(sr => sr.Vehicle)
                .ThenInclude(v => v.Customer)
            .Where(sr => sr.ServiceDate >= startDate.Date && sr.ServiceDate <= endDate.Date)
            .OrderByDescending(sr => sr.ServiceDate)
            .ThenByDescending(sr => sr.Id)
            .ToListAsync(ct);
    }

    public async Task<List<ServiceRecord>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        return await _context.ServiceRecords
            .AsNoTracking()
            .Include(sr => sr.Technician)
            .Include(sr => sr.Vehicle)
                .ThenInclude(v => v.Customer)
            .OrderByDescending(sr => sr.ServiceDate)
            .ThenByDescending(sr => sr.Id)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> GetTodayVehicleCountAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return await _context.ServiceRecords
            .Where(sr => sr.ServiceDate == today)
            .Select(sr => sr.VehicleId)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<ServiceRecord> AddAsync(ServiceRecord record, CancellationToken ct = default)
    {
        _context.ServiceRecords.Add(record);
        return record;
    }

    public async Task AddRangeAsync(IEnumerable<ServiceRecord> records, CancellationToken ct = default)
    {
        _context.ServiceRecords.AddRange(records);
    }

    public async Task UpdateAsync(ServiceRecord record, CancellationToken ct = default)
    {
        _context.ServiceRecords.Update(record);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var record = await _context.ServiceRecords.FindAsync(new object[] { id }, ct);
        if (record != null)
        {
            _context.ServiceRecords.Remove(record);
        }
    }
}
