using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class PersonnelRepository : IPersonnelRepository
{
    private readonly AppDbContext _context;

    public PersonnelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Personnel>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Personnel
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);
    }

    public async Task<Personnel> AddAsync(Personnel personnel, CancellationToken ct = default)
    {
        _context.Personnel.Add(personnel);
        await _context.SaveChangesAsync(ct);
        return personnel;
    }

    public async Task UpdateAsync(Personnel personnel, CancellationToken ct = default)
    {
        _context.Personnel.Update(personnel);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var personnel = await _context.Personnel.FindAsync(new object[] { id }, ct);
        if (personnel != null)
        {
            _context.Personnel.Remove(personnel);
            await _context.SaveChangesAsync(ct);
        }
    }
}
