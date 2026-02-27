using BulentOtoElektrik.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BulentOtoElektrik.Infrastructure.Data.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseSeeder>? _logger;

    public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedExpenseCategoriesAsync();
        await SeedTechniciansAsync();
    }

    private async Task SeedExpenseCategoriesAsync()
    {
        if (await _context.ExpenseCategories.AnyAsync()) return;

        var categories = new List<ExpenseCategory>
        {
            new() { Name = "Kira" },
            new() { Name = "Elektrik" },
            new() { Name = "Su" },
            new() { Name = "Doğalgaz" },
            new() { Name = "Malzeme/Yedek Parça" },
            new() { Name = "Personel Maaş" },
            new() { Name = "Personel Günlük" },
            new() { Name = "Sigorta" },
            new() { Name = "Vergi" },
            new() { Name = "Ulaşım" },
            new() { Name = "Yemek" },
            new() { Name = "Diğer" }
        };

        _context.ExpenseCategories.AddRange(categories);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Seeded {Count} expense categories", categories.Count);
    }

    private async Task SeedTechniciansAsync()
    {
        if (await _context.Technicians.AnyAsync()) return;

        var technicians = new List<Technician>
        {
            new() { FullName = "İRFAN USTA" },
            new() { FullName = "ARDA K.AVCI" }
        };

        _context.Technicians.AddRange(technicians);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Seeded {Count} technicians", technicians.Count);
    }

}
