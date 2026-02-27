using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class TechnicianRepository : ITechnicianRepository
{
    private readonly AppDbContext _context;

    public TechnicianRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Technician>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Technicians
            .AsNoTracking()
            .OrderBy(t => t.FullName)
            .ToListAsync(ct);
    }

    public async Task<List<Technician>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _context.Technicians
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .ToListAsync(ct);
    }

    public async Task<Technician> AddAsync(Technician technician, CancellationToken ct = default)
    {
        _context.Technicians.Add(technician);
        await _context.SaveChangesAsync(ct);
        return technician;
    }

    public async Task UpdateAsync(Technician technician, CancellationToken ct = default)
    {
        var tracked = _context.ChangeTracker.Entries<Technician>()
            .FirstOrDefault(e => e.Entity.Id == technician.Id);

        if (tracked != null)
        {
            tracked.CurrentValues.SetValues(technician);
        }
        else
        {
            _context.Technicians.Update(technician);
        }

        await _context.SaveChangesAsync(ct);
    }
}
