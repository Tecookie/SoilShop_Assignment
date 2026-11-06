using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record ProductInput([Required, MaxLength(40)] string Code, [Required, MaxLength(200)] string Name,
    [MaxLength(150)] string? Brand = null, [MaxLength(5000)] string? Ingredients = null,
    [MaxLength(2000)] string? SourceReference = null, long? ExpectedVersion = null, bool IsActive = true, [Required] string ProductType = ProductTypes.Unclassified);

public sealed record ProductDto(int Id, string Code, string Name, string? Brand, string? Ingredients, string? SourceReference, bool IsActive, long Version, string ProductType);

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
