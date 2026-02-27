using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vehicle>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Customer)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync(ct);
    }

    public async Task<Vehicle?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.ServiceRecords)
                .ThenInclude(sr => sr.Technician)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<Vehicle?> GetByPlateAsync(string plateNumber, CancellationToken ct = default)
    {
        return await _context.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.PlateNumber == plateNumber, ct);
    }

    public async Task<List<VehicleSearchResult>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return new();
        var term = searchTerm.ToLower(TurkishCulture);

        // SQLite cannot Sum() on decimal in projections, so materialize first
        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Customer)
                .ThenInclude(c => c.Payments)
            .Include(v => v.ServiceRecords)
            .Where(v =>
                EF.Functions.Like(v.PlateNumber.ToLower(), $"%{term}%") ||
                EF.Functions.Like(v.Customer.FullName.ToLower(), $"%{term}%"))
            .Take(20)
            .ToListAsync(ct);

        return vehicles.Select(v => new VehicleSearchResult
        {
            VehicleId = v.Id,
            CustomerId = v.CustomerId,
            PlateNumber = v.PlateNumber,
            CustomerName = v.Customer.FullName,
            VehicleModel = $"{v.VehicleBrand} {v.VehicleModel} {v.VehicleYear}".Trim(),
            Balance = v.ServiceRecords.Sum(sr => sr.TotalAmount) -
                      v.Customer.Payments.Sum(p => p.Amount)
        }).ToList();
    }

    public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .Include(v => v.ServiceRecords)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync(ct);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var vehicle = await _context.Vehicles.FindAsync(new object[] { id }, ct);
        if (vehicle != null)
        {
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync(ct);
        }
    }
}
