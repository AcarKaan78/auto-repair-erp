using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using BulentOtoElektrik.Infrastructure.Repositories;

namespace BulentOtoElektrik.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private ICustomerRepository? _customers;
    private IVehicleRepository? _vehicles;
    private IServiceRecordRepository? _serviceRecords;
    private IPaymentRepository? _payments;
    private IDailyExpenseRepository? _dailyExpenses;
    private IExpenseCategoryRepository? _expenseCategories;
    private ITechnicianRepository? _technicians;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
    public IVehicleRepository Vehicles => _vehicles ??= new VehicleRepository(_context);
    public IServiceRecordRepository ServiceRecords => _serviceRecords ??= new ServiceRecordRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IDailyExpenseRepository DailyExpenses => _dailyExpenses ??= new DailyExpenseRepository(_context);
    public IExpenseCategoryRepository ExpenseCategories => _expenseCategories ??= new ExpenseCategoryRepository(_context);
    public ITechnicianRepository Technicians => _technicians ??= new TechnicianRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
