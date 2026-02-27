using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Tests.Helpers;

namespace BulentOtoElektrik.Tests.Repositories;

public class VehicleRepositoryTests
{
    [Fact]
    public async Task SearchAsync_FindsByPlatePartialMatch()
    {
        // Use InMemory provider because SearchAsync uses Sum on decimal
        using var context = TestDbContextFactory.CreateInMemory();

        var customer = new Customer { FullName = "Test Müşteri" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        context.Vehicles.Add(new Vehicle { CustomerId = customer.Id, PlateNumber = "31 ALT 559" });
        context.Vehicles.Add(new Vehicle { CustomerId = customer.Id, PlateNumber = "34 ABC 123" });
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var results = await repo.SearchAsync("ALT");

        Assert.Single(results);
        Assert.Equal("31 ALT 559", results[0].PlateNumber);
    }

    [Fact]
    public async Task SearchAsync_FindsByCustomerName()
    {
        using var context = TestDbContextFactory.CreateInMemory();

        var customer = new Customer { FullName = "AHMET YILMAZ" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        context.Vehicles.Add(new Vehicle { CustomerId = customer.Id, PlateNumber = "06 XY 123" });
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var results = await repo.SearchAsync("ahmet");

        Assert.Single(results);
        Assert.Equal("AHMET YILMAZ", results[0].CustomerName);
    }

    [Fact]
    public async Task GetByPlateAsync_ReturnsCorrectVehicle()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var customer = new Customer { FullName = "Müşteri" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            context.Vehicles.Add(new Vehicle { CustomerId = customer.Id, PlateNumber = "31 ALT 559" });
            await context.SaveChangesAsync();

            var repo = new VehicleRepository(context);
            var result = await repo.GetByPlateAsync("31 ALT 559");

            Assert.NotNull(result);
            Assert.Equal("31 ALT 559", result.PlateNumber);
            Assert.NotNull(result.Customer);
            Assert.Equal("Müşteri", result.Customer.FullName);
        }
    }

    [Fact]
    public async Task GetByPlateAsync_ReturnsNullForNonExistent()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new VehicleRepository(context);
            var result = await repo.GetByPlateAsync("99 ZZZ 999");
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyCustomerVehicles()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var c1 = new Customer { FullName = "Müşteri 1" };
            var c2 = new Customer { FullName = "Müşteri 2" };
            context.Customers.AddRange(c1, c2);
            await context.SaveChangesAsync();

            context.Vehicles.Add(new Vehicle { CustomerId = c1.Id, PlateNumber = "06 A 001" });
            context.Vehicles.Add(new Vehicle { CustomerId = c1.Id, PlateNumber = "06 A 002" });
            context.Vehicles.Add(new Vehicle { CustomerId = c2.Id, PlateNumber = "34 B 001" });
            await context.SaveChangesAsync();

            var repo = new VehicleRepository(context);
            var result = await repo.GetByCustomerIdAsync(c1.Id);

            Assert.Equal(2, result.Count);
            Assert.All(result, v => Assert.Equal(c1.Id, v.CustomerId));
        }
    }

    [Fact]
    public async Task AddAsync_SetsIdAndTimestamps()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var customer = new Customer { FullName = "Test" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var repo = new VehicleRepository(context);
            var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "01 AB 123" };
            var result = await repo.AddAsync(vehicle);

            Assert.True(result.Id > 0);
            Assert.NotEqual(default, result.CreatedAt);
        }
    }
}
