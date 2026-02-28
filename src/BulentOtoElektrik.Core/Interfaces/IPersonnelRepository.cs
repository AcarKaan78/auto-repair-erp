using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IPersonnelRepository
{
    Task<List<Personnel>> GetAllAsync(CancellationToken ct = default);
    Task<Personnel> AddAsync(Personnel personnel, CancellationToken ct = default);
    Task UpdateAsync(Personnel personnel, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
