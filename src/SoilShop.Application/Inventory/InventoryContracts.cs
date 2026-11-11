using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record MovementDto(long Id, int SkuId, string Kind, int DeltaQty, int BalanceAfter, long StockVersionAfter, DateTime CreatedAt);

public sealed record SuggestionDto(int Id, int SkuId, int? SupplierId, int ObservedQty, int SuggestedQty, string Status, DateTime UpdatedAt, long Version, int? ReceiptId);
