using System.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SoilShop.Application;

namespace SoilShop.Infrastructure;

public sealed class AdviceCandidateRepository(ShopDbContext db, IMapper mapper) : IAdviceCandidateRepository
{
    public async Task<IReadOnlyList<ProductAdviceCandidate>> CandidatesAsync(ProductAdviceInput input, CancellationToken ct)
    {
        // Consistent review/metadata/tag snapshot; complete database reads before the network RPC.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var query = from sku in db.Skus.AsNoTracking() join product in db.Products.AsNoTracking() on sku.ProductId equals product.Id
                    where sku.IsActive && product.IsActive && sku.StockQty > 0 && product.ReviewedAt != null && product.ReviewedBy != null
                        && product.SourceReference != null && product.SourceReference.Trim() != ""
                    select new { Sku = sku, Product = product };
        if (input.MaxPriceVnd is decimal budget) query = query.Where(x => x.Sku.SalePriceVnd <= budget);
        if (input.PackageUnit is string unit) query = query.Where(x => x.Sku.PackageUnit == unit);
        if (input.PackageAmount is decimal amount) query = query.Where(x => x.Sku.PackageAmount == amount);
        var rows = await query.OrderBy(x => x.Sku.Id).Take(501).ToListAsync(ct);
        var ids = rows.Select(x => x.Product.Id).Distinct().ToArray();
        var tags = await (from link in db.ProductPlantTags.AsNoTracking() join tag in db.PlantTags.AsNoTracking() on link.PlantTagId equals tag.Id where ids.Contains(link.ProductId) select new { link.ProductId, tag.Code }).ToListAsync(ct);
        var groups = tags.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(t => t.Code).OrderBy(x => x).ToList());
        var results = rows.Select(x =>
        {
            var metadata = mapper.Map<SoilMetadataDto>(x.Product); metadata.PlantGroups = groups.GetValueOrDefault(x.Product.Id) ?? [];
            return new ProductAdviceCandidate(mapper.Map<SkuDto>(x.Sku), x.Product.Name, metadata);
        }).ToList();
        await tx.CommitAsync(ct); return results;
    }
}
