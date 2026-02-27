using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Tests.Helpers;

namespace BulentOtoElektrik.Tests.Repositories;

public class ServiceRecordRepositoryTests
{
    private async Task<(int vehicleId, int technicianId)> SeedVehicleAndTechnician(Infrastructure.Data.AppDbContext context)
    {
        var customer = new Customer { FullName = "Test Müşteri" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "31 TEST 01" };
        context.Vehicles.Add(vehicle);

        var technician = new Technician { FullName = "Test Usta", IsActive = true };
        context.Technicians.Add(technician);
        await context.SaveChangesAsync();

        return (vehicle.Id, technician.Id);
    }

    [Fact]
    public async Task AddAsync_AutoComputesTotalAmount()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var (vehicleId, _) = await SeedVehicleAndTechnician(context);

            var repo = new ServiceRecordRepository(context);
            var record = new ServiceRecord
            {
                VehicleId = vehicleId,
                WorkPerformed = "Motor tamiri",
                Quantity = 3,
                UnitPrice = 100m,
                ServiceDate = DateTime.Today
            };

            var result = await repo.AddAsync(record);

            Assert.Equal(300m, result.TotalAmount);
        }
    }

    [Fact]
    public async Task GetByVehicleIdAsync_ReturnsOrderedByDate()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var (vehicleId, techId) = await SeedVehicleAndTechnician(context);
            var repo = new ServiceRecordRepository(context);

            await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, TechnicianId = techId,
                WorkPerformed = "İş 2", Quantity = 1, UnitPrice = 200,
                ServiceDate = new DateTime(2025, 3, 15)
            });
            await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, TechnicianId = techId,
                WorkPerformed = "İş 1", Quantity = 1, UnitPrice = 100,
                ServiceDate = new DateTime(2025, 3, 10)
            });

            var results = await repo.GetByVehicleIdAsync(vehicleId);

            Assert.Equal(2, results.Count);
            Assert.True(results[0].ServiceDate <= results[1].ServiceDate);
            Assert.NotNull(results[0].Technician);
        }
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var (vehicleId, _) = await SeedVehicleAndTechnician(context);
            var repo = new ServiceRecordRepository(context);

            await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, WorkPerformed = "Ocak işi",
                Quantity = 1, UnitPrice = 100, ServiceDate = new DateTime(2025, 1, 15)
            });
            await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, WorkPerformed = "Mart işi",
                Quantity = 1, UnitPrice = 200, ServiceDate = new DateTime(2025, 3, 15)
            });
            await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, WorkPerformed = "Haziran işi",
                Quantity = 1, UnitPrice = 300, ServiceDate = new DateTime(2025, 6, 15)
            });

            var results = await repo.GetByDateRangeAsync(
                new DateTime(2025, 2, 1), new DateTime(2025, 4, 30));

            Assert.Single(results);
            Assert.Equal("Mart işi", results[0].WorkPerformed);
        }
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsLastNRecords()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var (vehicleId, _) = await SeedVehicleAndTechnician(context);
            var repo = new ServiceRecordRepository(context);

            for (int i = 1; i <= 5; i++)
            {
                await repo.AddAsync(new ServiceRecord
                {
                    VehicleId = vehicleId, WorkPerformed = $"İş {i}",
                    Quantity = 1, UnitPrice = i * 100,
                    ServiceDate = DateTime.Today.AddDays(-i)
                });
            }

            var results = await repo.GetRecentAsync(3);

            Assert.Equal(3, results.Count);
            // Most recent first
            Assert.True(results[0].ServiceDate >= results[1].ServiceDate);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var (vehicleId, _) = await SeedVehicleAndTechnician(context);
            var repo = new ServiceRecordRepository(context);

            var record = await repo.AddAsync(new ServiceRecord
            {
                VehicleId = vehicleId, WorkPerformed = "Silinecek",
                Quantity = 1, UnitPrice = 50, ServiceDate = DateTime.Today
            });

            await repo.DeleteAsync(record.Id);

            var results = await repo.GetByVehicleIdAsync(vehicleId);
            Assert.Empty(results);
        }
    }
}
