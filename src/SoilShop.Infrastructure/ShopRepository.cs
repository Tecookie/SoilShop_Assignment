using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed partial class ShopRepository(ShopDbContext db) : IShopRepository
{
    public async Task<PageResult<Product>> ProductsAsync(string? search, bool? active, string sort, int page, int size, CancellationToken ct, string? productType = null)
    {
        var q = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.ToLower().Contains(search.ToLower()) || x.Code.ToLower().Contains(search.ToLower()));
        if (active.HasValue) q = q.Where(x => x.IsActive == active.Value);
        if (productType is not null) q = q.Where(x => x.ProductType == productType);
        var total = await q.CountAsync(ct);
        q = sort switch { "code" => q.OrderBy(x => x.Code).ThenBy(x => x.Id), "newest" => q.OrderByDescending(x => x.Id), _ => q.OrderBy(x => x.Name).ThenBy(x => x.Id) };
        return new(await q.Skip((page - 1) * size).Take(size).ToListAsync(ct), total, page, size);
    }
    public Task<Product?> ProductAsync(int id, CancellationToken ct) => db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<Product> SaveProductAsync(Product product, bool create, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (create) db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        if (!create)
            foreach (var id in await db.Skus.Where(x => x.ProductId == product.Id).Select(x => x.Id).ToListAsync(ct)) AddInventoryEvent(id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return product;
    }
    public async Task DeleteProductAsync(Product product, CancellationToken ct) { db.Products.Remove(product); await db.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<Supplier>> SuppliersAsync(CancellationToken ct) => await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Take(100).ToListAsync(ct);
    public async Task<Supplier> AddSupplierAsync(Supplier supplier, CancellationToken ct) { db.Suppliers.Add(supplier); await db.SaveChangesAsync(ct); return supplier; }
    public Task<bool> SupplierActiveAsync(int id, CancellationToken ct) => db.Suppliers.AnyAsync(x => x.Id == id && x.IsActive, ct);
    public async Task<PageResult<Sku>> SkusAsync(string? search, bool lowOnly, int? productId, int page, int size, CancellationToken ct, string? productType = null)
    {
        var q = db.Skus.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Code.ToLower().Contains(search.ToLower()) || db.Products.Any(p => p.Id == x.ProductId && p.Name.ToLower().Contains(search.ToLower())));
        if (lowOnly) q = q.Where(x => x.IsActive && x.StockQty <= x.LowStockThreshold);
        if (productId.HasValue) q = q.Where(x => x.ProductId == productId);
        if (productType is not null) q = q.Where(x => db.Products.Any(p => p.Id == x.ProductId && p.ProductType == productType));
        var total = await q.CountAsync(ct);
        return new(await q.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct), total, page, size);
    }
    public Task<Sku?> SkuAsync(int id, CancellationToken ct) => db.Skus.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<Sku> SaveSkuAsync(Sku sku, bool create, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (create) db.Skus.Add(sku);
        await db.SaveChangesAsync(ct); AddInventoryEvent(sku.Id); await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct); return sku;
    }
    public Task<User?> FindUserAsync(string username, CancellationToken ct) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Username == username, ct);
    public Task<User?> UserAsync(int id, CancellationToken ct) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct)
    {
        var q = db.StockMovements.AsNoTracking(); if (skuId.HasValue) q = q.Where(x => x.SkuId == skuId);
        var total = await q.CountAsync(ct);
        return new(await q.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size).Select(x => new MovementDto(x.Id, x.SkuId, x.Kind, x.DeltaQty, x.BalanceAfter, x.StockVersionAfter, x.CreatedAt)).ToListAsync(ct), total, page, size);
    }
    public async Task<IReadOnlyList<RestockSuggestion>> SuggestionsAsync(CancellationToken ct) => await db.RestockSuggestions.AsNoTracking().Where(x => x.Status != "Resolved").OrderBy(x => x.SkuId).Take(100).ToListAsync(ct);
    private void AddInventoryEvent(int skuId) => db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), Kind = "InventoryChanged", Payload = JsonSerializer.Serialize(new { skuId }) });
}
