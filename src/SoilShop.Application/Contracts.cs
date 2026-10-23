using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record LoginInput([Required, MaxLength(100)] string Username, [Required, MaxLength(200)] string Password);
public sealed record UserDto(int Id, string Username, string DisplayName, string Role);
public sealed record LoginResult(string Token, DateTime ExpiresAt, UserDto User);
public sealed record ProductInput([Required, MaxLength(40)] string Code, [Required, MaxLength(200)] string Name,
    [MaxLength(150)] string? Brand = null, [MaxLength(5000)] string? Ingredients = null,
    [MaxLength(2000)] string? SourceReference = null, long? ExpectedVersion = null, bool IsActive = true);
public sealed record ProductDto(int Id, string Code, string Name, string? Brand, string? Ingredients, string? SourceReference, bool IsActive, long Version);
public sealed record SupplierInput([Required, MaxLength(40)] string Code, [Required, MaxLength(200)] string Name, [MaxLength(2000)] string? ContactNote = null);
public sealed record SupplierDto(int Id, string Code, string Name, string? ContactNote, bool IsActive);
public sealed record SkuInput([Range(1, int.MaxValue)] int ProductId, [Required, MaxLength(50)] string Code,
    [Range(typeof(decimal), "0.01", "1000000")] decimal PackageAmount, [Required, RegularExpression("^(kg|L)$")] string PackageUnit,
    [Range(typeof(decimal), "1", "1000000000000")] decimal SalePriceVnd,
    [Range(0, 1000000)] int LowStockThreshold = 5, [Range(1, 1000000)] int TargetStock = 30,
    int? PreferredSupplierId = null, long? ExpectedVersion = null, bool IsActive = true);
public sealed record SkuDto(int Id, int ProductId, string Code, decimal PackageAmount, string PackageUnit,
    decimal SalePriceVnd, int StockQty, long StockVersion, int LowStockThreshold, int TargetStock,
    int? PreferredSupplierId, bool IsActive, long Version);
public sealed record TradeLineInput([Range(1, int.MaxValue)] int SkuId, [Range(1, 1000000)] int Quantity, decimal? UnitCostVnd = null);
public sealed record TradeInput([Required, MinLength(1), MaxLength(100)] List<TradeLineInput> Lines, int? SupplierId = null);
public sealed record ConfirmInput(Guid ActionKey, long ExpectedVersion);
public sealed record TradeLineDto(int SkuId, int Quantity, decimal? UnitPriceVnd, string? Label);
public sealed record TradeDto(int Id, string Code, string Status, int? SupplierId, decimal? TotalVnd,
    long Version, DateTime CreatedAt, IReadOnlyList<TradeLineDto> Lines);
public sealed record MovementDto(long Id, int SkuId, string Kind, int DeltaQty, int BalanceAfter, long StockVersionAfter, DateTime CreatedAt);
public sealed record SuggestionDto(int Id, int SkuId, int? SupplierId, int ObservedQty, int SuggestedQty, string Status, DateTime UpdatedAt);
public sealed record CalculationLine(int Quantity, decimal Price);
public interface ICalculationClient { Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct); }
public sealed class BusinessException(int status, string message) : Exception(message) { public int Status { get; } = status; }

public interface IShopRepository
{
    Task<PageResult<Product>> ProductsAsync(string? search, bool? active, string sort, int page, int size, CancellationToken ct);
    Task<Product?> ProductAsync(int id, CancellationToken ct);
    Task<Product> SaveProductAsync(Product product, bool create, CancellationToken ct);
    Task DeleteProductAsync(Product product, CancellationToken ct);
    Task<IReadOnlyList<Supplier>> SuppliersAsync(CancellationToken ct);
    Task<Supplier> AddSupplierAsync(Supplier supplier, CancellationToken ct);
    Task<bool> SupplierActiveAsync(int id, CancellationToken ct);
    Task<PageResult<Sku>> SkusAsync(string? search, bool lowOnly, int? productId, int page, int size, CancellationToken ct);
    Task<Sku?> SkuAsync(int id, CancellationToken ct);
    Task<Sku> SaveSkuAsync(Sku sku, bool create, CancellationToken ct);
    Task<User?> FindUserAsync(string username, CancellationToken ct);
    Task<User?> UserAsync(int id, CancellationToken ct);
    Task<TradeDto> CreateTradeAsync(bool receipt, TradeInput input, int actorId, CancellationToken ct);
    Task<TradeDto?> TradeAsync(bool receipt, int id, CancellationToken ct);
    Task<PageResult<TradeDto>> TradesAsync(bool receipt, int page, int size, CancellationToken ct);
    Task<IReadOnlyList<CalculationLine>> QuoteAsync(bool receipt, int id, CancellationToken ct);
    Task<TradeDto> ConfirmAsync(bool receipt, int id, ConfirmInput input, decimal quote, int actorId, CancellationToken ct);
    Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct);
    Task<IReadOnlyList<RestockSuggestion>> SuggestionsAsync(CancellationToken ct);
}
public interface IPasswordVerifier { bool Verify(User user, string password); }
public interface ITokenIssuer { LoginResult Issue(User user); }
