using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SoilShop.Application;
using Xunit;

namespace SoilShop.Tests;

public sealed class ProductAdviceTests(ShopFactory factory) : IClassFixture<ShopFactory>
{
    [Fact]
    public async Task Advice_requires_review_stock_and_budget_and_edit_invalidates_eligibility()
    {
        using var c = factory.CreateClient();
        var auth = await c.PostAsJsonAsync("/api/auth/login", new { username = "test-admin", password = "Isolated-test-password-738!" });
        var session = (await auth.Content.ReadFromJsonAsync<LoginResult>())!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        async Task<T> Post<T>(string url, object data)
        {
            var response = await c.PostAsJsonAsync(url, data); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>())!;
        }
        var code = Guid.NewGuid().ToString("N");
        var product = await Post<ProductDto>("/api/products", new { code, name = "Advice synthetic fixture" });
        var sku = await Post<SkuDto>("/api/skus", new { code, productId = product.Id, packageAmount = 5, packageUnit = "L", salePriceVnd = 50000 });
        var path = $"/api/products/{product.Id}/metadata";
        var input = new SoilMetadataInput(product.Version, Drainage: "High", Aeration: "High", WaterRetention: "Medium", SourceReference: "Synthetic isolated test label", PlantGroups: ["Foliage"]);
        var savedResponse = await c.PutAsJsonAsync(path, input); savedResponse.EnsureSuccessStatusCode();
        var saved = (await savedResponse.Content.ReadFromJsonAsync<SoilMetadataDto>())!;
        var preferences = new ProductAdviceInput("Foliage", "High", "High", "Medium", MaxPriceVnd: 60000, PackageUnit: "L", PackageAmount: 5);
        Assert.DoesNotContain((await Post<ProductAdviceResponse>("/api/product-advice", preferences)).Results, x => x.Candidate.Sku.Id == sku.Id);
        var reviewed = await Post<SoilMetadataDto>(path + "/review", new { expectedVersion = saved.Version });
        Assert.DoesNotContain((await Post<ProductAdviceResponse>("/api/product-advice", preferences)).Results, x => x.Candidate.Sku.Id == sku.Id);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<TradingService>();
            var supplier = await scope.ServiceProvider.GetRequiredService<CatalogueService>().AddSupplierAsync(new(code, "Test supplier"), default);
            var receipt = await service.CreateTradeAsync(true, new([new(sku.Id, 3, 30000)], supplier.Id), session.User.Id, default);
            await service.ConfirmAsync(true, receipt.Id, new(Guid.NewGuid(), receipt.Version), session.User.Id, default);
        }
        var result = await Post<ProductAdviceResponse>("/api/product-advice", preferences);
        var match = Assert.Single(result.Results, x => x.Candidate.Sku.Id == sku.Id);
        Assert.Equal(12, match.Score); Assert.Equal(12, match.MaxScore); Assert.Equal(reviewed.Version, match.Candidate.Metadata.Version); Assert.NotEmpty(match.Reasons);
        Assert.DoesNotContain((await Post<ProductAdviceResponse>("/api/product-advice", preferences with { MaxPriceVnd = 10 })).Results, x => x.Candidate.Sku.Id == sku.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/product-advice", new { maxPriceVnd = 50000 })).StatusCode);
        var edited = await c.PutAsJsonAsync(path, input with { ExpectedVersion = reviewed.Version, Aeration = "Medium" }); edited.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await Post<ProductAdviceResponse>("/api/product-advice", preferences)).Results, x => x.Candidate.Sku.Id == sku.Id);
    }
}
