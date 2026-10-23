using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoilShop.Domain;
using SoilShop.Infrastructure;
using SoilShop.Application;
using Xunit;

namespace SoilShop.Tests;

public sealed class ShopFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Run scripts/test.ps1 to supply an isolated PostgreSQL database.");
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Shop"] = connection,
            ["Jwt:Key"] = "ISOLATED_TEST_SIGNING_KEY_01234567890123456789",
            ["Bootstrap:Username"] = "test-admin",
            ["Bootstrap:Password"] = "Isolated-test-password-738!",
            ["Messaging:Enabled"] = "false"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICalculationClient>();
            services.AddSingleton<ICalculationClient, TestCalculation>();
        });
    }
    private sealed class TestCalculation : ICalculationClient
    {
        public Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct) =>
            Task.FromResult(lines.Sum(x => x.Quantity * x.Price));
    }
}

public sealed class TradingTests : IClassFixture<ShopFactory>
{
    private readonly ShopFactory factory;
    public TradingTests(ShopFactory factory) => this.factory = factory;

    private async Task<HttpClient> Login()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "test-admin", password = "Isolated-test-password-738!" });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }
    private static async Task<T> Post<T>(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {content}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task<SkuDto> Sku(HttpClient c)
    {
        var code = Guid.NewGuid().ToString("N");
        var product = await Post<ProductDto>(c, "/api/products", new { code, name = "Test pre-packed soil" });
        return await Post<SkuDto>(c, "/api/skus", new { productId = product.Id, code, packageAmount = 5, packageUnit = "kg", salePriceVnd = 50000, lowStockThreshold = 5, targetStock = 30 });
    }
    private static async Task<TradeDto> Draft(HttpClient c, string kind, object[] lines, int? supplierId = null) =>
        await Post<TradeDto>(c, $"/api/{kind}", new { supplierId, lines });
    private static async Task Receive(HttpClient c, int skuId, int quantity)
    {
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Test supplier" });
        var draft = await Draft(c, "purchase-receipts", [new { skuId, quantity, unitCostVnd = 30000 }], supplier.Id);
        await Post<TradeDto>(c, $"/api/purchase-receipts/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version });
    }
    [Fact]
    public async Task Anonymous_cannot_read_inventory()
    {
        using var c = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/skus")).StatusCode);
    }
    [Fact]
    public async Task Client_stock_and_totals_cannot_override_service_values()
    {
        using var c = await Login();
        var sku = await Sku(c);
        Assert.Equal(0, sku.StockQty);
        var bad = await c.PostAsJsonAsync("/api/skus", new { productId = sku.ProductId, code = "BAD", packageAmount = 1, packageUnit = "kg", salePriceVnd = 1, stockQty = 999 });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }
    [Fact]
    public async Task Last_bag_has_one_winner_and_duplicate_action_has_one_effect()
    {
        using var c = await Login();
        var sku = await Sku(c);
        await Receive(c, sku.Id, 1);
        var a = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var b = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var keyA = Guid.NewGuid(); var keyB = Guid.NewGuid();
        var results = await Task.WhenAll(
            c.PostAsJsonAsync($"/api/sales/{a.Id}/confirm", new { actionKey = keyA, expectedVersion = a.Version }),
            c.PostAsJsonAsync($"/api/sales/{b.Id}/confirm", new { actionKey = keyB, expectedVersion = b.Version }));
        Assert.Single(results, x => x.IsSuccessStatusCode);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        var winner = results[0].IsSuccessStatusCode ? a : b;
        var key = results[0].IsSuccessStatusCode ? keyA : keyB;
        var repeated = await Post<TradeDto>(c, $"/api/sales/{winner.Id}/confirm", new { actionKey = key, expectedVersion = winner.Version });
        Assert.Equal(50000, repeated.TotalVnd);
        Assert.Equal(0, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
        var movements = await c.GetFromJsonAsync<PageResult<MovementDto>>($"/api/stock-movements?skuId={sku.Id}");
        Assert.Equal(2, movements!.Total);
    }
    [Fact]
    public async Task Multiline_shortage_rolls_back_all_stock_and_document_state()
    {
        using var c = await Login(); var a = await Sku(c); var b = await Sku(c);
        await Receive(c, a.Id, 2);
        var draft = await Draft(c, "sales", [new { skuId = a.Id, quantity = 1 }, new { skuId = b.Id, quantity = 1 }]);
        var result = await c.PostAsJsonAsync($"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version });
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal(2, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{a.Id}"))!.StockQty);
        Assert.Equal("Draft", (await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}"))!.Status);
    }
    [Fact]
    public async Task Nonwhole_money_and_duplicate_sku_lines_are_rejected()
    {
        using var c = await Login(); var sku = await Sku(c);
        var response = await c.PostAsJsonAsync("/api/sales", new { lines = new[] { new { skuId = sku.Id, quantity = 1 }, new { skuId = sku.Id, quantity = 1 } } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Supplier" });
        response = await c.PostAsJsonAsync("/api/purchase-receipts", new { supplierId = supplier.Id, lines = new[] { new { skuId = sku.Id, quantity = 1, unitCostVnd = 0.5 } } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Employee_cannot_maintain_catalogue_and_deactivation_revokes_existing_token()
    {
        using var admin = await Login();
        var username = "employee-" + Guid.NewGuid().ToString("N");
        var user = new User { Username = username, DisplayName = "Test employee", Role = "Employee" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Employee-test-password-738!");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            db.Users.Add(user); await db.SaveChangesAsync();
        }
        using var c = factory.CreateClient();
        var result = await Post<LoginResult>(c, "/api/auth/login", new { username, password = "Employee-test-password-738!" });
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/skus")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/products", new { code = "ATTEMPT", name = "Forbidden" })).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            await db.Users.Where(x => x.Id == user.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/skus")).StatusCode);
    }
    [Fact]
    public async Task Catalogue_updates_reject_stale_versions_and_case_duplicate_codes()
    {
        using var c = await Login();
        var code = Guid.NewGuid().ToString("N");
        var p = await Post<ProductDto>(c, "/api/products", new { code, name = "Initial" });
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/products", new { code = code.ToLowerInvariant(), name = "Duplicate" })).StatusCode);
        var updated = await c.PutAsJsonAsync($"/api/products/{p.Id}", new { code, name = "Updated", expectedVersion = p.Version });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var stale = await c.PutAsJsonAsync($"/api/products/{p.Id}", new { code, name = "Stale", expectedVersion = p.Version });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("Updated", (await c.GetFromJsonAsync<ProductDto>($"/api/products/{p.Id}"))!.Name);
    }
    [Fact]
    public async Task Confirmed_sale_keeps_price_snapshot_after_catalogue_price_changes()
    {
        using var c = await Login(); var sku = await Sku(c); await Receive(c, sku.Id, 2);
        var draft = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var key = Guid.NewGuid();
        await Post<TradeDto>(c, $"/api/sales/{draft.Id}/confirm", new { actionKey = key, expectedVersion = draft.Version });
        var edit = await c.PutAsJsonAsync($"/api/skus/{sku.Id}", new { sku.ProductId, sku.Code, sku.PackageAmount, sku.PackageUnit, salePriceVnd = 80000, sku.LowStockThreshold, sku.TargetStock, expectedVersion = sku.Version });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var frozen = await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}");
        Assert.Equal(50000, frozen!.TotalVnd); Assert.Equal(50000, frozen.Lines[0].UnitPriceVnd);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Equal(1, await db.StockMovements.CountAsync(x => x.SaleId == draft.Id));
        Assert.True(await db.OutboxMessages.AnyAsync(x => x.Kind == "InventoryChanged"));
    }
    [Fact]
    public void Entity_to_dto_mapping_configuration_is_valid() => factory.Services.GetRequiredService<AutoMapper.IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
}
