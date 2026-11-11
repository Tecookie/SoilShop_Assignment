using SoilShop.Domain;
namespace SoilShop.Application;

public sealed class TradingService(ITradingRepository repository, ICalculationClient calculation)
{
    public async Task<TradeDto> CreateTradeAsync(bool receipt, TradeInput input, int actorId, CancellationToken ct)
    {
        input = await NormalizeTradeAsync(receipt, input, ct);
        return await repository.CreateTradeAsync(receipt, input, actorId, ct);
    }

    private async Task<TradeInput> NormalizeTradeAsync(bool receipt, TradeInput input, CancellationToken ct)
    {
        if (input.Lines is null || input.Lines.Count is < 1 or > 100) throw new BusinessException(400, "Use 1–100 SKU lines.");
        if (receipt && (input.SupplierId is not int supplier || !await repository.SupplierActiveAsync(supplier, ct))) throw new BusinessException(400, "An active supplier is required.");
        if (!receipt && (input.SupplierId is not null || input.Lines.Any(x => x.UnitCostVnd is not null))) throw new BusinessException(400, "Sales use current catalogue prices; omit supplier and costs.");
        foreach (var line in input.Lines)
        {
            if (line.Quantity is < 1 or > 1000000 || line.SkuId < 1) throw new BusinessException(400, "Invalid SKU or bag quantity.");
            if (receipt && line.UnitCostVnd is decimal cost && (cost <= 0 || cost > 1000000000000m || decimal.Truncate(cost) != cost)) throw new BusinessException(400, "Cost must be positive whole VND.");
        }
        var merged = new List<TradeLineInput>();
        foreach (var group in input.Lines.GroupBy(x => x.SkuId))
        {
            if (group.Select(x => x.UnitCostVnd).Distinct().Count() != 1) throw new BusinessException(400, "Duplicate SKU lines must use the same purchase cost, including unknown costs.");
            var quantity = group.Sum(x => (long)x.Quantity);
            if (quantity > 1000000) throw new BusinessException(400, "Merged bag quantity exceeds 1,000,000.");
            merged.Add(new(group.Key, (int)quantity, group.First().UnitCostVnd));
        }
        return new(merged, input.SupplierId);
    }

    public async Task<TradeDto> UpdateTradeAsync(bool receipt, int id, UpdateTradeInput input, CancellationToken ct)
    {
        if (input.ExpectedVersion < 1) throw new BusinessException(400, "Expected version is required.");
        var normalized = await NormalizeTradeAsync(receipt, new(input.Lines, input.SupplierId), ct);
        return await repository.UpdateTradeAsync(receipt, id, normalized, input.ExpectedVersion, ct);
    }

    public Task DeleteTradeAsync(bool receipt, int id, long expectedVersion, CancellationToken ct)
    {
        if (expectedVersion < 1) throw new BusinessException(400, "Expected version is required.");
        return repository.DeleteTradeAsync(receipt, id, expectedVersion, ct);
    }

    public async Task<TradeQuote> QuoteAsync(bool receipt, int id, CancellationToken ct)
    {
        var quote = await repository.QuoteAsync(receipt, id, ct);
        var total = await calculation.TotalAsync(quote.Lines.Select(x => new CalculationLine(x.Quantity, x.UnitPriceVnd)).ToList(), ct);
        return quote with { TotalVnd = total };
    }

    public async Task<TradeDto> TradeAsync(bool receipt, int id, CancellationToken ct) => await repository.TradeAsync(receipt, id, ct) ?? throw new BusinessException(404, "Document not found.");

    public Task<PageResult<TradeDto>> TradesAsync(bool receipt, int page, int size, CancellationToken ct,TradeFilter? filter=null)
    {
        Pagination.Validate(page,size);
        if(filter?.Status is not (null or "" or "Draft" or "Confirmed" or "Cancelled")) throw new BusinessException(400,"Unknown document status.");
        if(filter?.From>filter?.To) throw new BusinessException(400,"Start date must not follow end date.");
        if(filter?.To==DateOnly.MaxValue) throw new BusinessException(400,"End date exceeds the supported range.");
        return repository.TradesAsync(receipt,page,size,ct,filter);
    }

    public async Task<TradeDto> ConfirmAsync(bool receipt, int id, ConfirmInput input, int actorId, CancellationToken ct)
    {
        if (input.ActionKey == Guid.Empty || input.ExpectedVersion < 1) throw new BusinessException(400, "Action key and expected version are required.");
        var existing = await TradeAsync(receipt, id, ct);
        // Repository still checks the key under the document lock for confirmed retry requests.
        TradeQuote? quote = null;
        if (existing.Status == "Draft")
        {
            quote = await QuoteAsync(receipt, id, ct);
            if (input.ExpectedTotalVnd is decimal expected && expected != quote.TotalVnd)
                throw new QuoteConflictException("Prices changed. Review the refreshed quote before confirming.", quote);
        }
        try { return await repository.ConfirmAsync(receipt, id, input, quote, actorId, ct); }
        catch (PriceChangedException)
        {
            throw new QuoteConflictException("Prices changed during calculation. Review the refreshed quote.", await QuoteAsync(receipt, id, ct));
        }
    }

    public Task<TradeDto> CancelTradeAsync(bool receipt, int id, CancelTradeInput input, int actorId, CancellationToken ct)
    {
        if (input.ActionKey == Guid.Empty || input.ExpectedVersion < 1 || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 500)
            throw new BusinessException(400, "Action key, expected version and a reason of 1–500 characters are required.");
        return repository.CancelTradeAsync(receipt, id, input with { Reason = input.Reason.Trim() }, actorId, ct);
    }
}
