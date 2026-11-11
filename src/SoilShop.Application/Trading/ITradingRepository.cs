using SoilShop.Domain;

namespace SoilShop.Application;

public interface ITradingRepository
{
    Task<TradeDto> CreateTradeAsync(bool receipt, TradeInput input, int actorId, CancellationToken ct);
    Task<TradeDto?> TradeAsync(bool receipt, int id, CancellationToken ct);
    Task<PageResult<TradeDto>> TradesAsync(bool receipt, int page, int size, CancellationToken ct, TradeFilter? filter=null);
    Task<TradeDto> UpdateTradeAsync(bool receipt, int id, TradeInput input, long expectedVersion, CancellationToken ct);
    Task DeleteTradeAsync(bool receipt, int id, long expectedVersion, CancellationToken ct);
    Task<TradeQuote> QuoteAsync(bool receipt, int id, CancellationToken ct);
    Task<TradeDto> ConfirmAsync(bool receipt, int id, ConfirmInput input, TradeQuote? quote, int actorId, CancellationToken ct);
    Task<TradeDto> CancelTradeAsync(bool receipt, int id, CancelTradeInput input, int actorId, CancellationToken ct);
    Task<bool> SupplierActiveAsync(int id, CancellationToken ct);
}
