using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Infrastructure.Data;
using BulentOtoElektrik.Infrastructure.Services;
using BulentOtoElektrik.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Tests.Services;

public class ReportingServiceTests
{
    private static IServiceProvider BuildServiceProvider(AppDbContext context)
    {
        var services = new ServiceCollection();
        services.AddScoped<AppDbContext>(_ => context);
        return services.BuildServiceProvider();
    }

    private async Task SeedData(Infrastructure.Data.AppDbContext context, DateTime date)
    {
        var customer = new Customer { FullName = "Rapor Müşteri" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "31 RPR 01" };
        context.Vehicles.Add(vehicle);

        var tech = new Technician { FullName = "Usta 1", IsActive = true };
        context.Technicians.Add(tech);

        var category = new ExpenseCategory { Name = "Kira", IsActive = true };
        context.ExpenseCategories.Add(category);
        await context.SaveChangesAsync();

        // 3 service records on target date: 100 + 200 + 300 = 600
        for (int i = 1; i <= 3; i++)
        {
            context.ServiceRecords.Add(new ServiceRecord
            {
                VehicleId = vehicle.Id,
                TechnicianId = tech.Id,
                WorkPerformed = $"İş {i}",
                Quantity = 1,
                UnitPrice = i * 100m,
                ServiceDate = date
            });
        }

        // 2 expenses on target date: 50 + 75 = 125
        context.DailyExpenses.Add(new DailyExpense
        {
            CategoryId = category.Id, Description = "Gider 1",
            Amount = 50m, ExpenseDate = date
        });
        context.DailyExpenses.Add(new DailyExpense
        {
            CategoryId = category.Id, Description = "Gider 2",
            Amount = 75m, ExpenseDate = date
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDailySummaryAsync_ReturnsCorrectTotals()
    {
        // Use InMemory provider because ReportingService uses SumAsync on decimal
        using var context = TestDbContextFactory.CreateInMemory();
        var date = new DateTime(2025, 5, 15);
        await SeedData(context, date);

        var service = new ReportingService(BuildServiceProvider(context));
        var summary = await service.GetDailySummaryAsync(date);

        // 100 + 200 + 300 = 600
        Assert.Equal(600m, summary.TotalRevenue);
        // 50 + 75 = 125
        Assert.Equal(125m, summary.TotalExpenses);
        Assert.Equal(1, summary.VehicleCount);
        Assert.Equal(date, summary.Date);
    }

    [Fact]
    public async Task GetPeriodReportAsync_CalculatesDailyBreakdown()
    {
        using var context = TestDbContextFactory.CreateInMemory();
        var date1 = new DateTime(2025, 5, 10);
        var date2 = new DateTime(2025, 5, 12);
        await SeedData(context, date1);

        // Add extra data on a different date
        var vehicle = context.Vehicles.First();
        context.ServiceRecords.Add(new ServiceRecord
        {
            VehicleId = vehicle.Id, WorkPerformed = "Ek iş",
            Quantity = 2, UnitPrice = 150m, ServiceDate = date2
        });
        await context.SaveChangesAsync();

        var service = new ReportingService(BuildServiceProvider(context));
        var report = await service.GetPeriodReportAsync(date1, date2);

        Assert.Equal(date1, report.StartDate);
        Assert.Equal(date2, report.EndDate);
        // 600 (date1) + 300 (date2) = 900
        Assert.Equal(900m, report.TotalRevenue);
        Assert.Equal(125m, report.TotalExpenses);

        // Should have 2 daily entries
        Assert.Equal(2, report.DailyBreakdown.Count);
    }

    [Fact]
    public async Task GetTechnicianReportAsync_GroupsByTechnician()
    {
        using var context = TestDbContextFactory.CreateInMemory();

        var customer = new Customer { FullName = "Müşteri" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "01 TK 01" };
        context.Vehicles.Add(vehicle);

        var tech1 = new Technician { FullName = "İrfan Usta", IsActive = true };
        var tech2 = new Technician { FullName = "Arda K.", IsActive = true };
        context.Technicians.AddRange(tech1, tech2);
        await context.SaveChangesAsync();

        var date = new DateTime(2025, 5, 1);

        // 2 records for tech1: 500 + 300 = 800
        context.ServiceRecords.Add(new ServiceRecord
        {
            VehicleId = vehicle.Id, TechnicianId = tech1.Id,
            WorkPerformed = "İş A", Quantity = 1, UnitPrice = 500, ServiceDate = date
        });
        context.ServiceRecords.Add(new ServiceRecord
        {
            VehicleId = vehicle.Id, TechnicianId = tech1.Id,
            WorkPerformed = "İş B", Quantity = 1, UnitPrice = 300, ServiceDate = date
        });

        // 1 record for tech2: 200
        context.ServiceRecords.Add(new ServiceRecord
        {
            VehicleId = vehicle.Id, TechnicianId = tech2.Id,
            WorkPerformed = "İş C", Quantity = 1, UnitPrice = 200, ServiceDate = date
        });
        await context.SaveChangesAsync();

        var service = new ReportingService(BuildServiceProvider(context));
        var report = await service.GetTechnicianReportAsync(date, date);

        Assert.Equal(2, report.Count);
        Assert.Equal("İrfan Usta", report[0].TechnicianName); // Higher revenue first
        Assert.Equal(800m, report[0].TotalRevenue);
        Assert.Equal(2, report[0].ServiceCount);
        Assert.Equal("Arda K.", report[1].TechnicianName);
    }

    [Fact]
    public async Task GetExpenseBreakdownAsync_CalculatesPercentages()
    {
        using var context = TestDbContextFactory.CreateInMemory();

        var cat1 = new ExpenseCategory { Name = "Kira", IsActive = true };
        var cat2 = new ExpenseCategory { Name = "Elektrik", IsActive = true };
        context.ExpenseCategories.AddRange(cat1, cat2);
        await context.SaveChangesAsync();

        var date = new DateTime(2025, 5, 1);

        context.DailyExpenses.Add(new DailyExpense
        {
            CategoryId = cat1.Id, Description = "Kira", Amount = 750m, ExpenseDate = date
        });
        context.DailyExpenses.Add(new DailyExpense
        {
            CategoryId = cat2.Id, Description = "Fatura", Amount = 250m, ExpenseDate = date
        });
        await context.SaveChangesAsync();

        var service = new ReportingService(BuildServiceProvider(context));
        var breakdown = await service.GetExpenseBreakdownAsync(date, date);

        Assert.Equal(2, breakdown.Count);
        var totalPercentage = breakdown.Sum(b => b.Percentage);
        Assert.Equal(100.0, totalPercentage, precision: 1);
        Assert.Equal("Kira", breakdown[0].CategoryName); // Highest amount first
        Assert.Equal(75.0, breakdown[0].Percentage, precision: 1);
    }
}
