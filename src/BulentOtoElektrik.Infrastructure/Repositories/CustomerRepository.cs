using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;
    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);
    }

    public async Task<List<Customer>> GetAllWithDetailsAsync(CancellationToken ct = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceRecords)
            .Include(c => c.Payments)
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Customer?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceRecords)
                    .ThenInclude(sr => sr.Technician)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Customer>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return new();
        var term = searchTerm.ToLower(TurkishCulture);
        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Vehicles)
            .Where(c =>
                EF.Functions.Like(c.FullName.ToLower(), $"%{term}%") ||
                c.Vehicles.Any(v => EF.Functions.Like(v.PlateNumber.ToLower(), $"%{term}%")))
            .OrderBy(c => c.FullName)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        return customer;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }
    }

    public async Task<List<Customer>> GetTopDebtorsAsync(int count = 10, CancellationToken ct = default)
    {
        var customers = await _context.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceRecords)
            .Include(c => c.Payments)
            .ToListAsync(ct);
        return customers
            .Where(c => c.Balance > 0)
            .OrderByDescending(c => c.Balance)
            .Take(count)
            .ToList();
    }
}
