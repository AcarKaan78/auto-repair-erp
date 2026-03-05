using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly IServiceProvider _serviceProvider;

    public ReportingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<DailySummaryDto> GetDailySummaryAsync(DateTime date, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var targetDate = date.Date;
        var targetDateEnd = targetDate.AddDays(1);
        var previousDate = targetDate.AddDays(-1);
        var previousDateEnd = targetDate; // == previousDate.AddDays(1)

        // SQLite cannot Sum() on decimal, so materialize first then sum in memory
        // Revenue = actual payments received (not service record amounts)
        var todayPaymentAmounts = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= targetDate && p.PaymentDate < targetDateEnd)
            .Select(p => p.Amount)
            .ToListAsync(ct);
        var todayRevenue = todayPaymentAmounts.Sum();

        // Vehicle count still from service records (vehicles serviced today)
        var vehicleCount = await _context.ServiceRecords
            .AsNoTracking()
            .Where(sr => sr.ServiceDate >= targetDate && sr.ServiceDate < targetDateEnd)
            .Select(sr => sr.VehicleId)
            .Distinct()
            .CountAsync(ct);

        var todayExpenseAmounts = await _context.DailyExpenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= targetDate && e.ExpenseDate < targetDateEnd)
            .Select(e => e.Amount)
            .ToListAsync(ct);
        var todayExpenses = todayExpenseAmounts.Sum();

        var yesterdayPaymentAmounts = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= previousDate && p.PaymentDate < previousDateEnd)
            .Select(p => p.Amount)
            .ToListAsync(ct);
        var yesterdayRevenue = yesterdayPaymentAmounts.Sum();

        var yesterdayExpenseAmounts = await _context.DailyExpenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= previousDate && e.ExpenseDate < previousDateEnd)
            .Select(e => e.Amount)
            .ToListAsync(ct);
        var yesterdayExpenses = yesterdayExpenseAmounts.Sum();

        return new DailySummaryDto
        {
            Date = targetDate,
            TotalRevenue = todayRevenue,
            TotalExpenses = todayExpenses,
            VehicleCount = vehicleCount,
            RevenueChangePercent = yesterdayRevenue > 0
                ? Math.Round((double)((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100), 1)
                : 0,
            ExpenseChangePercent = yesterdayExpenses > 0
                ? Math.Round((double)((todayExpenses - yesterdayExpenses) / yesterdayExpenses * 100), 1)
                : 0,
            NetChangePercent = (yesterdayRevenue - yesterdayExpenses) != 0
                ? Math.Round((double)(((todayRevenue - todayExpenses) - (yesterdayRevenue - yesterdayExpenses)) / Math.Abs(yesterdayRevenue - yesterdayExpenses) * 100), 1)
                : 0
        };
    }

    public async Task<PeriodReportDto> GetPeriodReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var start = startDate.Date;
        var end = endDate.Date;

        // Revenue = actual payments received (not service record amounts)
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
            .ToListAsync(ct);

        var expenses = await _context.DailyExpenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end)
            .ToListAsync(ct);

        var dailyBreakdown = new List<DailyBreakdownDto>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayRevenue = payments.Where(p => p.PaymentDate.Date == date).Sum(p => p.Amount);
            var dayExpenses = expenses.Where(e => e.ExpenseDate == date).Sum(e => e.Amount);

            if (dayRevenue > 0 || dayExpenses > 0)
            {
                dailyBreakdown.Add(new DailyBreakdownDto
                {
                    Date = date,
                    Revenue = dayRevenue,
                    Expenses = dayExpenses
                });
            }
        }

        return new PeriodReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalRevenue = payments.Sum(p => p.Amount),
            TotalExpenses = expenses.Sum(e => e.Amount),
            DailyBreakdown = dailyBreakdown
        };
    }

    public async Task<List<TechnicianReportDto>> GetTechnicianReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var start = startDate.Date;
        var end = endDate.Date;

        // SQLite cannot Sum() on decimal in GroupBy, so materialize first
        var records = await _context.ServiceRecords
            .AsNoTracking()
            .Where(sr => sr.ServiceDate >= start && sr.ServiceDate <= end && sr.TechnicianId != null)
            .Include(sr => sr.Technician)
            .ToListAsync(ct);

        return records
            .GroupBy(sr => sr.Technician!.FullName)
            .Select(g => new TechnicianReportDto
            {
                TechnicianName = g.Key,
                TotalRevenue = g.Sum(sr => sr.TotalAmount),
                ServiceCount = g.Count()
            })
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();
    }

    public async Task<List<ExpenseBreakdownDto>> GetExpenseBreakdownAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var start = startDate.Date;
        var end = endDate.Date;

        // SQLite cannot Sum() on decimal in GroupBy, so materialize first
        var rawExpenses = await _context.DailyExpenses
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end)
            .ToListAsync(ct);

        var expenses = rawExpenses
            .GroupBy(e => e.Category.Name)
            .Select(g => new ExpenseBreakdownDto
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(e => e.TotalAmount)
            .ToList();

        var total = expenses.Sum(e => e.TotalAmount);
        if (total > 0)
        {
            foreach (var expense in expenses)
            {
                expense.Percentage = (double)(expense.TotalAmount / total * 100);
            }
        }

        return expenses;
    }
}
