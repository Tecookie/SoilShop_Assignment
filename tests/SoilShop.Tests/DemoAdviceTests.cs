using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoilShop.Application;
using SoilShop.Infrastructure;
using Xunit;

namespace SoilShop.Tests;
public sealed class DemoAdviceTests(ShopFactory factory) : IClassFixture<ShopFactory>
{
    [Fact]
    public async Task Explicit_advice_demo_supports_General_and_rerun_preserves_edits_and_stock()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ShopService>();
        var advice = scope.ServiceProvider.GetRequiredService<ProductAdviceService>();
        var seed = new DemoAdviceSeeder(db, service);
        await seed.RunAsync();
        var result = await advice.RecommendAsync(new(PlantGroup: "General"), default);
        var match = Assert.Single(result.Results, x => x.Candidate.Sku.Code == "ADVICE-V1-P01-5L");
        Assert.Equal(3, match.Score); Assert.Contains("DỮ LIỆU GIẢ LẬP", match.Candidate.Metadata.SourceReference);
        Assert.Null(match.Candidate.Metadata.PhMin); Assert.Null(match.Candidate.Metadata.Drainage);
        var sku = await db.Skus.SingleAsync(x => x.Code == "ADVICE-V1-P01-5L");
        var product = await db.Products.SingleAsync(x => x.Id == sku.ProductId);
        var version = product.Version; var stockVersion = sku.StockVersion; var stock = sku.StockQty;
        var count = await db.StockMovements.CountAsync(x => x.SkuId == sku.Id);
        var metadata = scope.ServiceProvider.GetRequiredService<SoilMetadataService>();
        await metadata.SaveAsync(product.Id, new(version, SourceReference: "Staff changed source", PlantGroups: ["General"]), default);
        await seed.RunAsync();
        Assert.Null(product.ReviewedAt); Assert.Equal("Staff changed source", product.SourceReference);
        Assert.Equal(stock, sku.StockQty); Assert.Equal(stockVersion, sku.StockVersion);
        Assert.Equal(count, await db.StockMovements.CountAsync(x => x.SkuId == sku.Id));
        Assert.DoesNotContain((await advice.RecommendAsync(new(PlantGroup: "General"), default)).Results, x => x.Candidate.Sku.Id == sku.Id);
    }
}
