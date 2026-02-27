using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync(CancellationToken ct = default);
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Vehicle?> GetByPlateAsync(string plateNumber, CancellationToken ct = default);
    Task<List<VehicleSearchResult>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<List<Vehicle>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken ct = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
