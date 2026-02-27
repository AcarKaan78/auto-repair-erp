using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Infrastructure;
using BulentOtoElektrik.Infrastructure.Repositories;
using BulentOtoElektrik.Tests.Helpers;

namespace BulentOtoElektrik.Tests.Repositories;

public class StockItemRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistStockItem()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);
            var item = new StockItem
            {
                MaterialName = "Ampul 12V",
                StockQuantity = 50,
                RemainingQuantity = 50,
                UnitPrice = 25.50m,
                IsActive = true
            };

            var result = await repo.AddAsync(item);

            Assert.True(result.Id > 0);
            Assert.Equal("Ampul 12V", result.MaterialName);
            Assert.Equal(50, result.StockQuantity);
            Assert.Equal(50, result.RemainingQuantity);
        }
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActiveItems()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);

            await repo.AddAsync(new StockItem
            {
                MaterialName = "Sigorta 10A",
                StockQuantity = 100,
                RemainingQuantity = 80,
                UnitPrice = 5m,
                IsActive = true
            });

            await repo.AddAsync(new StockItem
            {
                MaterialName = "Silinen Malzeme",
                StockQuantity = 10,
                RemainingQuantity = 10,
                UnitPrice = 100m,
                IsActive = false
            });

            await repo.AddAsync(new StockItem
            {
                MaterialName = "Kablo 1.5mm",
                StockQuantity = 200,
                RemainingQuantity = 150,
                UnitPrice = 3m,
                IsActive = true
            });

            var activeItems = await repo.GetActiveAsync();

            Assert.Equal(2, activeItems.Count);
            Assert.All(activeItems, item => Assert.True(item.IsActive));
            Assert.DoesNotContain(activeItems, item => item.MaterialName == "Silinen Malzeme");
        }
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOrderedByMaterialName()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);

            await repo.AddAsync(new StockItem { MaterialName = "Zamk", StockQuantity = 10, RemainingQuantity = 10, IsActive = true });
            await repo.AddAsync(new StockItem { MaterialName = "Ampul", StockQuantity = 20, RemainingQuantity = 20, IsActive = true });
            await repo.AddAsync(new StockItem { MaterialName = "Kablo", StockQuantity = 30, RemainingQuantity = 30, IsActive = true });

            var items = await repo.GetActiveAsync();

            Assert.Equal("Ampul", items[0].MaterialName);
            Assert.Equal("Kablo", items[1].MaterialName);
            Assert.Equal("Zamk", items[2].MaterialName);
        }
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveAndInactiveItems()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);

            await repo.AddAsync(new StockItem { MaterialName = "Aktif Malzeme", StockQuantity = 10, RemainingQuantity = 10, IsActive = true });
            await repo.AddAsync(new StockItem { MaterialName = "Pasif Malzeme", StockQuantity = 5, RemainingQuantity = 5, IsActive = false });

            var allItems = await repo.GetAllAsync();

            Assert.Equal(2, allItems.Count);
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);
            var item = await repo.AddAsync(new StockItem
            {
                MaterialName = "Eski Isim",
                StockQuantity = 20,
                RemainingQuantity = 20,
                UnitPrice = 10m,
                IsActive = true
            });

            item.MaterialName = "Yeni Isim";
            item.UnitPrice = 15m;
            await repo.UpdateAsync(item);

            var updated = (await repo.GetActiveAsync()).First();
            Assert.Equal("Yeni Isim", updated.MaterialName);
            Assert.Equal(15m, updated.UnitPrice);
        }
    }

    [Fact]
    public async Task SoftDelete_ShouldHideFromActiveList()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);
            var item = await repo.AddAsync(new StockItem
            {
                MaterialName = "Silinecek",
                StockQuantity = 10,
                RemainingQuantity = 10,
                IsActive = true
            });

            item.IsActive = false;
            await repo.UpdateAsync(item);

            var active = await repo.GetActiveAsync();
            var all = await repo.GetAllAsync();

            Assert.Empty(active);
            Assert.Single(all);
        }
    }

    [Fact]
    public async Task Restock_ShouldIncreaseBothQuantities()
    {
        var (context, conn) = TestDbContextFactory.Create();
        using (conn)
        using (context)
        {
            var repo = new StockItemRepository(context);
            var item = await repo.AddAsync(new StockItem
            {
                MaterialName = "Sigorta 15A",
                StockQuantity = 50,
                RemainingQuantity = 10,
                UnitPrice = 8m,
                IsActive = true
            });

            item.StockQuantity += 30;
            item.RemainingQuantity += 30;
            await repo.UpdateAsync(item);

            var updated = (await repo.GetActiveAsync()).First();
            Assert.Equal(80, updated.StockQuantity);
            Assert.Equal(40, updated.RemainingQuantity);
        }
    }

    [Fact]
    public void TotalValue_ShouldComputeCorrectly()
    {
        var item = new StockItem
        {
            RemainingQuantity = 25,
            UnitPrice = 12.50m
        };

        Assert.Equal(312.50m, item.TotalValue);
    }

    [Fact]
    public async Task UnitOfWork_StockItems_ShouldWorkWithSaveChanges()
    {
        using var context = TestDbContextFactory.CreateInMemory();
        var uow = new UnitOfWork(context);

        var item = new StockItem
        {
            MaterialName = "Motor Yagi",
            StockQuantity = 100,
            RemainingQuantity = 100,
            UnitPrice = 250m,
            IsActive = true
        };

        await uow.StockItems.AddAsync(item);

        var items = await uow.StockItems.GetActiveAsync();
        Assert.Single(items);
        Assert.Equal("Motor Yagi", items[0].MaterialName);
    }

    [Fact]
    public async Task Summary_TotalStockValue_ShouldComputeInMemory()
    {
        using var context = TestDbContextFactory.CreateInMemory();
        var repo = new StockItemRepository(context);

        await repo.AddAsync(new StockItem { MaterialName = "A", StockQuantity = 10, RemainingQuantity = 10, UnitPrice = 100m, IsActive = true });
        await repo.AddAsync(new StockItem { MaterialName = "B", StockQuantity = 20, RemainingQuantity = 5, UnitPrice = 50m, IsActive = true });
        await repo.AddAsync(new StockItem { MaterialName = "C", StockQuantity = 30, RemainingQuantity = 0, UnitPrice = 200m, IsActive = true });

        var items = await repo.GetActiveAsync();
        var totalValue = items.Sum(s => s.RemainingQuantity * s.UnitPrice);
        var lowStockCount = items.Count(s => s.RemainingQuantity < 5);

        Assert.Equal(1250m, totalValue); // 10*100 + 5*50 + 0*200
        Assert.Equal(1, lowStockCount); // only C has 0 remaining
    }
}
