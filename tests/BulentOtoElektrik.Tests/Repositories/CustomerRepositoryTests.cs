using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Tests.Helpers;

namespace BulentOtoElektrik.Tests.Repositories;

public class CustomerRepositoryTests
{
    [Fact]
    public async Task AddAsync_SetsIdAndCreatedAt()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var customer = new Customer { FullName = "Test Müşteri" };

            var result = await repo.AddAsync(customer);

            Assert.True(result.Id > 0);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.UpdatedAt);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistent()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var result = await repo.GetByIdAsync(999);
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsExistingCustomer()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var customer = new Customer { FullName = "Ali Veli" };
            await repo.AddAsync(customer);

            var result = await repo.GetByIdAsync(customer.Id);

            Assert.NotNull(result);
            Assert.Equal("Ali Veli", result.FullName);
        }
    }

    [Fact]
    public async Task SearchAsync_EmptyTermReturnsEmpty()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var result = await repo.SearchAsync("");
            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task SearchAsync_FindsByNamePartialMatch()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            await repo.AddAsync(new Customer { FullName = "AHMET YILMAZ" });
            await repo.AddAsync(new Customer { FullName = "MEHMET KAYA" });

            var result = await repo.SearchAsync("ahmet");

            Assert.Single(result);
            Assert.Equal("AHMET YILMAZ", result[0].FullName);
        }
    }

    [Fact]
    public async Task SearchAsync_FindsByVehiclePlate()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var customer = new Customer { FullName = "KEMAL ARAS" };
            await repo.AddAsync(customer);

            context.Vehicles.Add(new Vehicle
            {
                CustomerId = customer.Id,
                PlateNumber = "31 ALT 559"
            });
            await context.SaveChangesAsync();

            var result = await repo.SearchAsync("31 ALT");

            Assert.Single(result);
            Assert.Equal("KEMAL ARAS", result[0].FullName);
        }
    }

    [Fact]
    public async Task GetTopDebtorsAsync_ReturnsOrderedByBalanceDesc()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);

            // Customer 1: 500 TL debt, 100 TL paid => 400 balance
            var c1 = new Customer { FullName = "Borçlu 1" };
            await repo.AddAsync(c1);
            var v1 = new Vehicle { CustomerId = c1.Id, PlateNumber = "06AA001" };
            context.Vehicles.Add(v1);
            await context.SaveChangesAsync();
            context.ServiceRecords.Add(new ServiceRecord
            {
                VehicleId = v1.Id, WorkPerformed = "İş 1",
                Quantity = 1, UnitPrice = 500, ServiceDate = DateTime.Today
            });
            context.Payments.Add(new Payment
            {
                CustomerId = c1.Id, Amount = 100, PaymentDate = DateTime.Today
            });
            await context.SaveChangesAsync();

            // Customer 2: 1000 TL debt, 200 TL paid => 800 balance
            var c2 = new Customer { FullName = "Borçlu 2" };
            await repo.AddAsync(c2);
            var v2 = new Vehicle { CustomerId = c2.Id, PlateNumber = "34BB002" };
            context.Vehicles.Add(v2);
            await context.SaveChangesAsync();
            context.ServiceRecords.Add(new ServiceRecord
            {
                VehicleId = v2.Id, WorkPerformed = "İş 2",
                Quantity = 1, UnitPrice = 1000, ServiceDate = DateTime.Today
            });
            context.Payments.Add(new Payment
            {
                CustomerId = c2.Id, Amount = 200, PaymentDate = DateTime.Today
            });
            await context.SaveChangesAsync();

            var debtors = await repo.GetTopDebtorsAsync(10);

            Assert.Equal(2, debtors.Count);
            Assert.Equal("Borçlu 2", debtors[0].FullName);
            Assert.Equal("Borçlu 1", debtors[1].FullName);
            Assert.True(debtors[0].Balance > debtors[1].Balance);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesCustomer()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            var customer = new Customer { FullName = "Silinecek" };
            await repo.AddAsync(customer);

            await repo.DeleteAsync(customer.Id);

            var result = await repo.GetByIdAsync(customer.Id);
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new CustomerRepository(context);
            await repo.AddAsync(new Customer { FullName = "Zeynep" });
            await repo.AddAsync(new Customer { FullName = "Ali" });
            await repo.AddAsync(new Customer { FullName = "Mehmet" });

            var all = await repo.GetAllAsync();

            Assert.Equal(3, all.Count);
            Assert.Equal("Ali", all[0].FullName);
        }
    }
}
