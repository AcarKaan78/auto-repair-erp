namespace BulentOtoElektrik.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }
    IVehicleRepository Vehicles { get; }
    IServiceRecordRepository ServiceRecords { get; }
    IPaymentRepository Payments { get; }
    IDailyExpenseRepository DailyExpenses { get; }
    IExpenseCategoryRepository ExpenseCategories { get; }
    ITechnicianRepository Technicians { get; }
    IStockItemRepository StockItems { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
