using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class InventoryRepository(ShopDbContext db) : IInventoryRepository
{
    public async Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct)
    {
        var q = db.StockMovements.AsNoTracking(); if (skuId.HasValue) q = q.Where(x => x.SkuId == skuId);
        var total = await q.CountAsync(ct);
        return new(await q.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size).Select(x => new MovementDto(x.Id, x.SkuId, x.Kind, x.DeltaQty, x.BalanceAfter, x.StockVersionAfter, x.CreatedAt)).ToListAsync(ct), total, page, size);
    }

    public async Task<IReadOnlyList<RestockSuggestion>> SuggestionsAsync(CancellationToken ct) => await db.RestockSuggestions.AsNoTracking().Where(x => x.Status != "Resolved").OrderBy(x => x.SkuId).Take(100).ToListAsync(ct);
    public async Task<PageResult<RestockSuggestion>> SuggestionsPageAsync(string? search,int page,int size,CancellationToken ct)
    {
        var query=db.RestockSuggestions.AsNoTracking().Where(x=>x.Status!="Resolved");
        if(!string.IsNullOrWhiteSpace(search))
        {
            var term=search.Trim().ToLower();
            query=query.Where(x=>db.Skus.Any(s=>s.Id==x.SkuId && (s.Code.ToLower().Contains(term) || db.Products.Any(p=>p.Id==s.ProductId && p.Name.ToLower().Contains(term)))));
        }
        return new(await query.OrderBy(x=>x.SkuId).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync(ct),await query.CountAsync(ct),page,size);
    }
}
