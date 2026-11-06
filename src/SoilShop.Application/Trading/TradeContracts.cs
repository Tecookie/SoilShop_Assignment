using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record TradeLineInput([Range(1, int.MaxValue)] int SkuId, [Range(1, 1000000)] int Quantity, decimal? UnitCostVnd = null);

public sealed record TradeInput([Required, MinLength(1), MaxLength(100)] List<TradeLineInput> Lines, int? SupplierId = null);

public sealed record ConfirmInput(Guid ActionKey, long ExpectedVersion, decimal? ExpectedTotalVnd = null);

public sealed record UpdateTradeInput(List<TradeLineInput> Lines, long ExpectedVersion, int? SupplierId = null);

public sealed record CancelTradeInput(Guid ActionKey, long ExpectedVersion, [Required, MaxLength(500)] string Reason);

public sealed record TradeQuoteLine(int SkuId, int Quantity, decimal UnitPriceVnd);

public sealed record TradeQuote(long Version, decimal TotalVnd, IReadOnlyList<TradeQuoteLine> Lines);

public sealed record TradeLineDto(int SkuId, int Quantity, decimal? UnitPriceVnd, string? Label);

public sealed record TradeDto(int Id, string Code, string Status, int? SupplierId, decimal? TotalVnd,
    long Version, DateTime CreatedAt, IReadOnlyList<TradeLineDto> Lines,
    int CreatedBy = 0, int? ConfirmedBy = null, DateTime? ConfirmedAt = null,
    int? CancelledBy = null, DateTime? CancelledAt = null, string? CancelReason = null);
