using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.Vehicle)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Add(payment);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Update(payment);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var payment = await _context.Payments.FindAsync(new object[] { id }, ct);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
        }
    }
}
