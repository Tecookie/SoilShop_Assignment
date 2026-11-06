
namespace SoilShop.Application;

public sealed class QuoteConflictException(string message, TradeQuote quote) : BusinessException(409, message)
{
    public TradeQuote Quote { get; } = quote;
}

public sealed class PriceChangedException() : BusinessException(409, "Prices changed during calculation. Review and confirm again.");
