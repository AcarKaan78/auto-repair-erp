using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Customer?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);
    Task<List<Customer>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<Customer> AddAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Customer customer, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<List<Customer>> GetTopDebtorsAsync(int count = 10, CancellationToken ct = default);
}
