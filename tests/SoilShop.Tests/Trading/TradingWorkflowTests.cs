using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoilShop.Application;
using SoilShop.Domain;
using SoilShop.Infrastructure;
using Xunit;

namespace SoilShop.Tests;

public sealed partial class TradingTests
{
    private sealed class UnavailableCalculation : ICalculationClient
    {
        public Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct) =>
            throw new BusinessException(503, "Calculation service is unavailable.");
    }

    [Fact]
    public async Task Unavailable_calculation_leaves_stock_document_ledger_and_outbox_unchanged()
    {
        using var c = await Login(); var sku = await Sku(c); await Receive(c, sku.Id, 2);
        var draft = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        using var unavailable = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICalculationClient>(); services.AddSingleton<ICalculationClient, UnavailableCalculation>();
        }));
        using var failing = unavailable.CreateClient();
        failing.DefaultRequestHeaders.Authorization = c.DefaultRequestHeaders.Authorization;
        var result = await failing.PostAsJsonAsync($"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(2, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
        Assert.Equal("Draft", (await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}"))!.Status);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.False(await db.StockMovements.AnyAsync(x => x.SaleId == draft.Id));
        Assert.Equal(1, await db.StockMovements.CountAsync(x => x.SkuId == sku.Id));
    }

    [Fact]
    public async Task Deleting_linked_receipt_draft_reopens_suggestion_without_inventory_effects()
    {
        using var c = await Login(); var sku = await Sku(c);
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Supplier" });
        var draft = await Draft(c, "purchase-receipts", [new { skuId = sku.Id, quantity = 2, unitCostVnd = 1000 }], supplier.Id);
        var suggestion = new RestockSuggestion { SkuId = sku.Id, SupplierId = supplier.Id, SuggestedQty = 2, Status = "ReceiptDrafted", ReceiptId = draft.Id };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>(); db.RestockSuggestions.Add(suggestion); await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/purchase-receipts/{draft.Id}?expectedVersion={draft.Version}")).StatusCode);
        await using var check = factory.Services.CreateAsyncScope();
        var restored = await check.ServiceProvider.GetRequiredService<ShopDbContext>().RestockSuggestions.SingleAsync(x => x.Id == suggestion.Id);
        Assert.Equal("Open", restored.Status); Assert.Null(restored.ReceiptId); Assert.Equal(suggestion.Version + 1, restored.Version);
        Assert.Equal(0, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
    }

    [Fact]
    public async Task Draft_edit_delete_and_unknown_receipt_cost_do_not_change_stock()
    {
        using var c = await Login(); var sku = await Sku(c);
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Supplier" });
        var draft = await Draft(c, "purchase-receipts", [new { skuId = sku.Id, quantity = 2 }], supplier.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/purchase-receipts/{draft.Id}/quote")).StatusCode);
        var update = new { supplierId = supplier.Id, expectedVersion = draft.Version, lines = new[] { new { skuId = sku.Id, quantity = 3, unitCostVnd = 25000 } } };
        var saved = await c.PutAsJsonAsync($"/api/purchase-receipts/{draft.Id}", update);
        saved.EnsureSuccessStatusCode(); var edited = (await saved.Content.ReadFromJsonAsync<TradeDto>())!;
        Assert.Equal(3, edited.Lines[0].Quantity); Assert.Equal(draft.Version + 1, edited.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/purchase-receipts/{draft.Id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/purchase-receipts/{draft.Id}?expectedVersion={draft.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/purchase-receipts/{draft.Id}?expectedVersion={edited.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/purchase-receipts/{draft.Id}")).StatusCode);
        Assert.Equal(0, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
        Assert.Equal(0, (await c.GetFromJsonAsync<PageResult<MovementDto>>($"/api/stock-movements?skuId={sku.Id}"))!.Total);
    }

    [Fact]
    public async Task Receipt_duplicates_merge_only_when_costs_agree()
    {
        using var c = await Login(); var sku = await Sku(c);
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Supplier" });
        var good = await Draft(c, "purchase-receipts", [new { skuId = sku.Id, quantity = 2, unitCostVnd = 1000 }, new { skuId = sku.Id, quantity = 3, unitCostVnd = 1000 }], supplier.Id);
        Assert.Single(good.Lines); Assert.Equal(5, good.Lines[0].Quantity);
        var bad = await c.PostAsJsonAsync("/api/purchase-receipts", new { supplierId = supplier.Id, lines = new[] { new { skuId = sku.Id, quantity = 1, unitCostVnd = 1000 }, new { skuId = sku.Id, quantity = 1, unitCostVnd = 2000 } } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Stale_expected_total_returns_refreshed_quote_and_leaves_draft_unchanged()
    {
        using var c = await Login(); var sku = await Sku(c); await Receive(c, sku.Id, 2);
        var draft = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var quote = await c.GetFromJsonAsync<TradeQuote>($"/api/sales/{draft.Id}/quote");
        Assert.Equal(50000, quote!.TotalVnd);
        var result = await c.PostAsJsonAsync($"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version, expectedTotalVnd = 40000 });
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(50000, body.GetProperty("quote").GetProperty("totalVnd").GetDecimal());
        Assert.Equal("Draft", (await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}"))!.Status);
        Assert.Equal(2, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
    }

    [Fact]
    public async Task Per_line_price_race_is_rejected_even_when_combined_total_is_unchanged()
    {
        using var c = await Login(); var a = await Sku(c); var b = await Sku(c);
        await Receive(c, a.Id, 1); await Receive(c, b.Id, 1);
        var draft = await Draft(c, "sales", [new { skuId = a.Id, quantity = 1 }, new { skuId = b.Id, quantity = 1 }]);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ITradingRepository>();
            var quote = await repo.QuoteAsync(false, draft.Id, default);
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            await db.Skus.Where(x => x.Id == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.SalePriceVnd, 40000m));
            await db.Skus.Where(x => x.Id == b.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.SalePriceVnd, 60000m));
            var actor = (await c.GetFromJsonAsync<UserDto>("/api/auth/me"))!.Id;
            await Assert.ThrowsAsync<PriceChangedException>(() => repo.ConfirmAsync(false, draft.Id, new(Guid.NewGuid(), draft.Version), quote with { TotalVnd = 100000 }, actor, default));
        }
        Assert.Equal(1, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{a.Id}"))!.StockQty);
        Assert.Equal(1, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{b.Id}"))!.StockQty);
        Assert.Equal("Draft", (await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}"))!.Status);
    }

    [Fact]
    public async Task Sale_cancellation_is_idempotent_preserves_history_and_allows_inactive_sku()
    {
        using var c = await Login(); var sku = await Sku(c); await Receive(c, sku.Id, 3);
        var draft = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 2 }]);
        var confirmed = await Post<TradeDto>(c, $"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            await db.Skus.Where(x => x.Id == sku.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }
        var input = new { actionKey = Guid.NewGuid(), expectedVersion = confirmed.Version, reason = "Customer cancelled entire sale" };
        var calls = await Task.WhenAll(c.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", input), c.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", input));
        Assert.All(calls, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        var cancelled = (await c.GetFromJsonAsync<TradeDto>($"/api/sales/{draft.Id}"))!;
        Assert.Equal("Cancelled", cancelled.Status); Assert.Equal(confirmed.TotalVnd, cancelled.TotalVnd);
        Assert.Equal(confirmed.ConfirmedAt, cancelled.ConfirmedAt); Assert.NotNull(cancelled.CancelledBy);
        Assert.Equal(confirmed.Lines[0], cancelled.Lines[0]); Assert.Equal(input.reason, cancelled.CancelReason);
        Assert.Equal(3, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
        var ledger = (await c.GetFromJsonAsync<PageResult<MovementDto>>($"/api/stock-movements?skuId={sku.Id}"))!;
        Assert.Single(ledger.Items, x => x.Kind == "SaleCancel");
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/sales/{draft.Id}?expectedVersion={cancelled.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", new { actionKey = Guid.NewGuid(), expectedVersion = cancelled.Version, input.reason })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", new { input.actionKey, input.expectedVersion, reason = "Another payload" })).StatusCode);
    }

    [Fact]
    public async Task Confirmed_documents_are_immutable_and_confirmation_replay_rejects_changed_values()
    {
        using var c = await Login(); var sku = await Sku(c); await Receive(c, sku.Id, 2);
        var draft = await Draft(c, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var actionKey = Guid.NewGuid();
        var confirmed = await Post<TradeDto>(c, $"/api/sales/{draft.Id}/confirm", new { actionKey, expectedVersion = draft.Version, expectedTotalVnd = 50000 });
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/sales/{draft.Id}", new { expectedVersion = confirmed.Version, lines = new[] { new { skuId = sku.Id, quantity = 2 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/sales/{draft.Id}/confirm", new { actionKey, expectedVersion = draft.Version, expectedTotalVnd = 1 })).StatusCode);
        var cancelled = await Post<TradeDto>(c, $"/api/sales/{draft.Id}/cancel", new { actionKey = Guid.NewGuid(), expectedVersion = confirmed.Version, reason = "Replay check" });
        var repeated = await Post<TradeDto>(c, $"/api/sales/{draft.Id}/confirm", new { actionKey, expectedVersion = draft.Version, expectedTotalVnd = 50000 });
        Assert.Equal("Cancelled", repeated.Status); Assert.Equal(cancelled.Version, repeated.Version);
        Assert.Equal(2, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{sku.Id}"))!.StockQty);
    }

    [Fact]
    public async Task Multiline_receipt_cancellation_shortage_rolls_back_every_line()
    {
        using var c = await Login(); var a = await Sku(c); var b = await Sku(c);
        var supplier = await Post<SupplierDto>(c, "/api/suppliers", new { code = Guid.NewGuid().ToString("N"), name = "Supplier" });
        var receipt = await Draft(c, "purchase-receipts", [new { skuId = a.Id, quantity = 2, unitCostVnd = 1000 }, new { skuId = b.Id, quantity = 2, unitCostVnd = 2000 }], supplier.Id);
        var confirmed = await Post<TradeDto>(c, $"/api/purchase-receipts/{receipt.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = receipt.Version });
        var sale = await Draft(c, "sales", [new { skuId = b.Id, quantity = 1 }]);
        var sold = await Post<TradeDto>(c, $"/api/sales/{sale.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = sale.Version });
        var input = new { actionKey = Guid.NewGuid(), expectedVersion = confirmed.Version, reason = "Wrong delivery" };
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/purchase-receipts/{receipt.Id}/cancel", input)).StatusCode);
        Assert.Equal(2, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{a.Id}"))!.StockQty);
        Assert.Equal(1, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{b.Id}"))!.StockQty);
        Assert.Equal("Confirmed", (await c.GetFromJsonAsync<TradeDto>($"/api/purchase-receipts/{receipt.Id}"))!.Status);
        await Post<TradeDto>(c, $"/api/sales/{sale.Id}/cancel", new { actionKey = Guid.NewGuid(), expectedVersion = sold.Version, reason = "Restore sale" });
        var cancelled = await Post<TradeDto>(c, $"/api/purchase-receipts/{receipt.Id}/cancel", input);
        Assert.Equal("Cancelled", cancelled.Status);
        await Post<TradeDto>(c, $"/api/purchase-receipts/{receipt.Id}/cancel", input);
        Assert.Equal(0, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{a.Id}"))!.StockQty);
        Assert.Equal(0, (await c.GetFromJsonAsync<SkuDto>($"/api/skus/{b.Id}"))!.StockQty);
    }

    [Fact]
    public async Task Employee_can_quote_but_cannot_cancel_and_reason_is_required()
    {
        using var admin = await Login(); var sku = await Sku(admin); await Receive(admin, sku.Id, 2);
        var draft = await Draft(admin, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        var confirmed = await Post<TradeDto>(admin, $"/api/sales/{draft.Id}/confirm", new { actionKey = Guid.NewGuid(), expectedVersion = draft.Version });
        var user = new User { Username = "employee-" + Guid.NewGuid().ToString("N"), DisplayName = "Employee", Role = "Employee" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Employee-test-password-738!");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>(); db.Users.Add(user); await db.SaveChangesAsync();
        }
        using var employee = factory.CreateClient();
        var login = await Post<LoginResult>(employee, "/api/auth/login", new { username = user.Username, password = "Employee-test-password-738!" });
        employee.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var input = new { actionKey = Guid.NewGuid(), expectedVersion = confirmed.Version, reason = "Whole sale reversal" };
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", input)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/sales/{draft.Id}/cancel", new { actionKey = Guid.NewGuid(), expectedVersion = confirmed.Version, reason = " " })).StatusCode);
        var own = await Draft(employee, "sales", [new { skuId = sku.Id, quantity = 1 }]);
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync($"/api/sales/{own.Id}/quote")).StatusCode);
    }
}
