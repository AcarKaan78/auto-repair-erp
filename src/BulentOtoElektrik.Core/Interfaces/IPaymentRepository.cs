using BulentOtoElektrik.Core.Entities;

namespace BulentOtoElektrik.Core.Interfaces;

public interface IPaymentRepository
{
    Task<List<Payment>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
