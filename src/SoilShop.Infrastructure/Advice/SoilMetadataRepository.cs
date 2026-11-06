using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed class SoilMetadataRepository(ShopDbContext db) : ISoilMetadataRepository
{
    public async Task<IReadOnlyList<string>> PlantGroupsAsync(int id, CancellationToken ct) => await
        (from link in db.ProductPlantTags join tag in db.PlantTags on link.PlantTagId equals tag.Id where link.ProductId == id orderby tag.Code select tag.Code).ToListAsync(ct);
    public async Task SaveMetadataAsync(Product product, IReadOnlyList<string>? groups, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct); // Optimistic version check precedes association replacement.
        if (groups is not null)
        {
            await db.ProductPlantTags.Where(x => x.ProductId == product.Id).ExecuteDeleteAsync(ct);
            foreach (var code in groups)
            {
                var name = SoilOptions.PlantGroups[code];
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO plant_tags (code, name) VALUES ({code}, {name}) ON CONFLICT (code) DO NOTHING", ct);
                var tag = await db.PlantTags.SingleAsync(x => x.Code == code, ct);
                db.ProductPlantTags.Add(new ProductPlantTag { ProductId = product.Id, PlantTagId = tag.Id });
            }
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
