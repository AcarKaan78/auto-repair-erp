using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace BulentOtoElektrik.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DailyExpense> DailyExpenses => Set<DailyExpense>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Technician> Technicians => Set<Technician>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.Now;
                entry.Entity.UpdatedAt = DateTime.Now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.Now;
            }
        }

        // Auto-compute TotalAmount for ServiceRecords
        var serviceEntries = ChangeTracker.Entries<ServiceRecord>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        foreach (var entry in serviceEntries)
        {
            entry.Entity.TotalAmount = entry.Entity.Quantity * entry.Entity.UnitPrice;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
