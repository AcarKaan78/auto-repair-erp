using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
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
        await SeedSampleDataAsync();
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

    private async Task SeedSampleDataAsync()
    {
        if (await _context.Customers.AnyAsync()) return;

        var customer = new Customer
        {
            FullName = "KEMAL ARAS"
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            CustomerId = customer.Id,
            PlateNumber = "31 ALT 559",
            VehicleBrand = "FORD",
            VehicleModel = "TRANSİT"
        };
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var irfan = await _context.Technicians.FirstAsync(t => t.FullName == "İRFAN USTA");
        var arda = await _context.Technicians.FirstAsync(t => t.FullName == "ARDA K.AVCI");

        var records = new List<ServiceRecord>
        {
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = irfan.Id,
                ServiceDate = new DateTime(2026, 1, 30),
                Complaint = "ISITMIYOR",
                WorkPerformed = "KALORİFER TEMİZLİĞİ",
                Quantity = 1,
                UnitPrice = 1750,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = irfan.Id,
                ServiceDate = new DateTime(2026, 1, 30),
                Complaint = "H4 AMPÜL",
                WorkPerformed = "H4 AMPÜL",
                Quantity = 1,
                UnitPrice = 400,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = arda.Id,
                ServiceDate = new DateTime(2026, 2, 10),
                Complaint = "SONDAJ",
                WorkPerformed = "ŞARJ DİNAMOSU",
                Quantity = 1,
                UnitPrice = 8350,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = arda.Id,
                ServiceDate = new DateTime(2026, 2, 10),
                Complaint = "ELEMEĞİ",
                WorkPerformed = "ELEMEĞİ",
                Quantity = 1,
                UnitPrice = 1500,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = irfan.Id,
                ServiceDate = new DateTime(2026, 2, 20),
                Complaint = "31 ALT 559",
                WorkPerformed = "KONTAK TERMİĞİ",
                Quantity = 1,
                UnitPrice = 900,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = irfan.Id,
                ServiceDate = new DateTime(2026, 2, 20),
                Complaint = "KOMPLE MARŞ",
                WorkPerformed = "KOMPLE MARŞ",
                Quantity = 1,
                UnitPrice = 6300,
                Currency = CurrencyType.TL
            },
            new()
            {
                VehicleId = vehicle.Id,
                TechnicianId = irfan.Id,
                ServiceDate = new DateTime(2026, 2, 20),
                Complaint = "ELEMEĞİ",
                WorkPerformed = "ELEMEĞİ",
                Quantity = 1,
                UnitPrice = 1500,
                Currency = CurrencyType.TL
            }
        };

        _context.ServiceRecords.AddRange(records);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Seeded sample customer with {Count} service records", records.Count);
    }
}
