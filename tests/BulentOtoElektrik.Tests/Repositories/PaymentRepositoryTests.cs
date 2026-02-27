using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Tests.Helpers;

namespace BulentOtoElektrik.Tests.Repositories;

public class PaymentRepositoryTests
{
    [Fact]
    public async Task AddAsync_SavesPaymentCorrectly()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var customer = new Customer { FullName = "Ödeme Müşteri" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var payment = new Payment
            {
                CustomerId = customer.Id,
                Amount = 500m,
                PaymentMethod = PaymentMethod.Cash,
                PaymentDate = DateTime.Today
            };

            var result = await repo.AddAsync(payment);

            Assert.True(result.Id > 0);
            Assert.Equal(500m, result.Amount);
            Assert.Equal(PaymentMethod.Cash, result.PaymentMethod);
        }
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOrderedByDateDesc()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var customer = new Customer { FullName = "Müşteri" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);

            await repo.AddAsync(new Payment
            {
                CustomerId = customer.Id, Amount = 100,
                PaymentDate = new DateTime(2025, 1, 10)
            });
            await repo.AddAsync(new Payment
            {
                CustomerId = customer.Id, Amount = 200,
                PaymentDate = new DateTime(2025, 3, 20)
            });
            await repo.AddAsync(new Payment
            {
                CustomerId = customer.Id, Amount = 150,
                PaymentDate = new DateTime(2025, 2, 15)
            });

            var results = await repo.GetByCustomerIdAsync(customer.Id);

            Assert.Equal(3, results.Count);
            Assert.True(results[0].PaymentDate >= results[1].PaymentDate);
            Assert.True(results[1].PaymentDate >= results[2].PaymentDate);
        }
    }

    [Fact]
    public async Task Balance_ComputedCorrectly()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            // Create customer with services and payments
            var customer = new Customer { FullName = "Bakiye Test" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var vehicle = new Vehicle { CustomerId = customer.Id, PlateNumber = "01 TEST 01" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            // 2 service records: 500 + 300 = 800 TL total debt
            context.ServiceRecords.Add(new ServiceRecord
            {
                VehicleId = vehicle.Id, WorkPerformed = "İş 1",
                Quantity = 1, UnitPrice = 500, ServiceDate = DateTime.Today
            });
            context.ServiceRecords.Add(new ServiceRecord
            {
                VehicleId = vehicle.Id, WorkPerformed = "İş 2",
                Quantity = 1, UnitPrice = 300, ServiceDate = DateTime.Today
            });

            // 1 payment: 350 TL
            context.Payments.Add(new Payment
            {
                CustomerId = customer.Id, Amount = 350, PaymentDate = DateTime.Today
            });

            await context.SaveChangesAsync();

            // Reload with details
            var customerRepo = new CustomerRepository(context);
            var loaded = await customerRepo.GetByIdWithDetailsAsync(customer.Id);

            Assert.NotNull(loaded);
            Assert.Equal(800m, loaded.TotalDebt);
            Assert.Equal(350m, loaded.TotalPayments);
            Assert.Equal(450m, loaded.Balance);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesPayment()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var customer = new Customer { FullName = "Sil Test" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var repo = new PaymentRepository(context);
            var payment = await repo.AddAsync(new Payment
            {
                CustomerId = customer.Id, Amount = 100, PaymentDate = DateTime.Today
            });

            await repo.DeleteAsync(payment.Id);

            var results = await repo.GetByCustomerIdAsync(customer.Id);
            Assert.Empty(results);
        }
    }
}
