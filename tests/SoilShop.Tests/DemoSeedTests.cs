using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoilShop.Application;
using SoilShop.Infrastructure;
using Xunit;

namespace SoilShop.Tests;

public sealed class DemoSeedTests(ShopFactory factory) : IClassFixture<ShopFactory>
{
    private async Task RunSeed(bool failCalculation = false)
    {
        // Start the host/bootstrap once before resolving scoped services.
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var service = failCalculation
            ? new ShopService(scope.ServiceProvider.GetRequiredService<IShopRepository>(), scope.ServiceProvider.GetRequiredService<IMapper>(), new UnavailableCalculation())
            : scope.ServiceProvider.GetRequiredService<ShopService>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DemoSeed:EmployeeUsername"] = "demo-employee",
            ["DemoSeed:EmployeePassword"] = "Demo-seed-test-password-738!"
        }).Build();
        await new DemoDataSeeder(db, service, config).RunAsync();
    }
    private sealed class UnavailableCalculation : ICalculationClient
    {
        public Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct) => throw new BusinessException(503, "Test calculation outage.");
    }
    [Fact]
    public async Task Seed_resumes_then_repeats_without_resetting_staff_changes_or_inventory()
    {
        await Assert.ThrowsAsync<BusinessException>(() => RunSeed(failCalculation: true));
        await RunSeed();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            Assert.Equal(8, await db.Products.CountAsync(p => p.Code.StartsWith("SEED-V1-")));
            Assert.Equal(16, await db.Skus.CountAsync(p => p.Code.StartsWith("SEED-V1-")));
            Assert.Equal(6, await db.Products.Where(p => p.Code.StartsWith("SEED-V1-")).Select(p => p.ProductType).Distinct().CountAsync());
            Assert.Equal(3, await db.PurchaseReceipts.CountAsync(x => x.Code.StartsWith("SEED-V1-")));
            Assert.Equal(4, await db.Sales.CountAsync(x => x.Code.StartsWith("SEED-V1-")));
            Assert.Equal(2, await db.PurchaseReceipts.CountAsync(x => x.Code.StartsWith("SEED-V1-") && x.Status == "Confirmed"));
            Assert.Equal("Employee", (await db.Users.SingleAsync(x => x.Username == "demo-employee")).Role);
            Assert.All(await db.Products.Where(p => p.Code.StartsWith("SEED-V1-")).ToListAsync(), p => { Assert.Null(p.PhMin); Assert.Null(p.ReviewedBy); });
        }
        var before = await Snapshot(); await RunSeed(); Assert.Equal(before, await Snapshot());
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            var sku = await db.Skus.Where(x => x.Code.StartsWith("SEED-V1-") && x.IsActive && x.StockQty > 5).FirstAsync();
            sku.SalePriceVnd = 123456; sku.Version++; await db.SaveChangesAsync();
            var actor = await db.Users.Where(x => x.Role == "Admin").Select(x => x.Id).FirstAsync();
            var service = scope.ServiceProvider.GetRequiredService<ShopService>();
            var draft = await service.CreateTradeAsync(false, new TradeInput([new(sku.Id, 1)]), actor, default);
            await service.ConfirmAsync(false, draft.Id, new(Guid.NewGuid(), draft.Version), actor, default);
        }
        before = await Snapshot(); await RunSeed(); Assert.Equal(before, await Snapshot());
    }
    private async Task<string> Snapshot()
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var ids = db.Skus.Where(x => x.Code.StartsWith("SEED-V1-")).Select(x => x.Id);
        var skus = await db.Skus.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).Select(x => new { x.Id, x.StockQty, x.StockVersion, x.SalePriceVnd }).ToListAsync();
        return System.Text.Json.JsonSerializer.Serialize(new { skus, movements = await db.StockMovements.CountAsync(x => ids.Contains(x.SkuId)), outbox = await db.OutboxMessages.CountAsync(), receipts = await db.PurchaseReceipts.CountAsync(), sales = await db.Sales.CountAsync() });
    }
}
