using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface ITechnicianRepository
{
    Task<List<Technician>> GetAllAsync(CancellationToken ct = default);
    Task<List<Technician>> GetActiveAsync(CancellationToken ct = default);
    Task<Technician> AddAsync(Technician technician, CancellationToken ct = default);
    Task UpdateAsync(Technician technician, CancellationToken ct = default);
}
