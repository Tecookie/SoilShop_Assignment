using SoilShop.Domain;

namespace SoilShop.Application;

public interface ICatalogueRepository
{
    Task<PageResult<Product>> ProductsAsync(string? search, bool? active, string sort, int page, int size, CancellationToken ct, string? productType = null);
    Task<Product?> ProductAsync(int id, CancellationToken ct);
    Task<Product> SaveProductAsync(Product product, bool create, CancellationToken ct);
    Task DeleteProductAsync(Product product, CancellationToken ct);
    Task<IReadOnlyList<Supplier>> SuppliersAsync(CancellationToken ct);
    Task<Supplier> AddSupplierAsync(Supplier supplier, CancellationToken ct);
    Task<bool> SupplierActiveAsync(int id, CancellationToken ct);
    Task<PageResult<Sku>> SkusAsync(string? search, bool lowOnly, int? productId, int page, int size, CancellationToken ct, string? productType = null);
    Task<Sku?> SkuAsync(int id, CancellationToken ct);
    Task<Sku> SaveSkuAsync(Sku sku, bool create, CancellationToken ct);
}
