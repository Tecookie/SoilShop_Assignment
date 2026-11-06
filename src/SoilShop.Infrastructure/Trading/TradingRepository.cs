using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed partial class TradingRepository(ShopDbContext db) : ITradingRepository
{
    public Task<bool> SupplierActiveAsync(int id, CancellationToken ct) => db.Suppliers.AnyAsync(x => x.Id == id && x.IsActive, ct);

    public async Task<TradeDto> CreateTradeAsync(bool receipt, TradeInput input, int actorId, CancellationToken ct)
    {
        var ids = input.Lines.Select(x => x.SkuId).ToArray();
        if (await db.Skus.CountAsync(x => ids.Contains(x.Id) && x.IsActive && db.Products.Any(p => p.Id == x.ProductId && p.IsActive), ct) != ids.Length) throw new BusinessException(400, "Some SKUs are unavailable.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        int id;
        if (receipt)
        {
            var doc = new PurchaseReceipt { Code = "IN-" + Guid.NewGuid().ToString("N"), SupplierId = input.SupplierId!.Value, CreatedBy = actorId };
            db.PurchaseReceipts.Add(doc); await db.SaveChangesAsync(ct); id = doc.Id;
            db.PurchaseReceiptLines.AddRange(input.Lines.Select(x => new PurchaseReceiptLine { ReceiptId = id, SkuId = x.SkuId, Quantity = x.Quantity, UnitCostVnd = x.UnitCostVnd }));
        }
        else
        {
            var doc = new Sale { Code = "OUT-" + Guid.NewGuid().ToString("N"), CreatedBy = actorId };
            db.Sales.Add(doc); await db.SaveChangesAsync(ct); id = doc.Id;
            db.SaleLines.AddRange(input.Lines.Select(x => new SaleLine { SaleId = id, SkuId = x.SkuId, Quantity = x.Quantity }));
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (await TradeAsync(receipt, id, ct))!;
    }
    public async Task<TradeDto?> TradeAsync(bool receipt, int id, CancellationToken ct)
    {
        if (receipt)
        {
            var doc = await db.PurchaseReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (doc is null) return null;
            var lines = await db.PurchaseReceiptLines.AsNoTracking().Where(x => x.ReceiptId == id).OrderBy(x => x.SkuId).Select(x => new TradeLineDto(x.SkuId, x.Quantity, x.UnitCostVnd, null)).ToListAsync(ct);
            return new(doc.Id, doc.Code, doc.Status, doc.SupplierId, doc.TotalVnd, doc.Version, doc.CreatedAt, lines,
                doc.CreatedBy, doc.ConfirmedBy, doc.ConfirmedAt, doc.CancelledBy, doc.CancelledAt, doc.CancelReason);
        }
        else
        {
            var doc = await db.Sales.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (doc is null) return null;
            var lines = await db.SaleLines.AsNoTracking().Where(x => x.SaleId == id).OrderBy(x => x.SkuId).Select(x => new TradeLineDto(x.SkuId, x.Quantity, x.UnitPriceVnd, x.SkuLabelSnapshot)).ToListAsync(ct);
            return new(doc.Id, doc.Code, doc.Status, null, doc.TotalVnd, doc.Version, doc.CreatedAt, lines,
                doc.CreatedBy, doc.ConfirmedBy, doc.ConfirmedAt, doc.CancelledBy, doc.CancelledAt, doc.CancelReason);
        }
    }
    public async Task<PageResult<TradeDto>> TradesAsync(bool receipt, int page, int size, CancellationToken ct)
    {
        var ids = receipt ? db.PurchaseReceipts.Select(x => x.Id) : db.Sales.Select(x => x.Id);
        var total = await ids.CountAsync(ct); var result = new List<TradeDto>();
        foreach (var id in await ids.OrderByDescending(x => x).Skip((page - 1) * size).Take(size).ToListAsync(ct)) result.Add((await TradeAsync(receipt, id, ct))!);
        return new(result, total, page, size);
    }
    public async Task<TradeQuote> QuoteAsync(bool receipt, int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var doc = await TradeAsync(receipt, id, ct) ?? throw new BusinessException(404, "Document not found.");
        if (doc.Status != "Draft") throw new BusinessException(409, "Only Draft documents can be quoted.");
        var result = new List<TradeQuoteLine>();
        foreach (var line in doc.Lines)
        {
            var price = receipt ? line.UnitPriceVnd : await db.Skus.Where(x => x.Id == line.SkuId).Select(x => (decimal?)x.SalePriceVnd).SingleAsync(ct);
            if (price is null) throw new BusinessException(400, "Set every receipt line cost before confirming.");
            result.Add(new(line.SkuId, line.Quantity, price.Value));
        }
        await tx.CommitAsync(ct);
        return new(doc.Version, 0, result);
    }
    public async Task<TradeDto> ConfirmAsync(bool receipt, int id, ConfirmInput input, TradeQuote? quote, int actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var state = await LockTradeAsync(receipt, id, input, actorId, ct);
        if (state is null) { await tx.CommitAsync(ct); return (await TradeAsync(receipt, id, ct))!; }
        var doc = await TradeAsync(receipt, id, ct) ?? throw new BusinessException(404, "Document not found.");
        if (doc.Lines.Count == 0) throw new BusinessException(400, "Document requires lines.");
        if (quote is null || quote.Version != doc.Version || quote.Lines.Count != doc.Lines.Count) throw new BusinessException(409, "Draft changed during calculation. Reload and quote again.");
        decimal total = 0;
        foreach (var line in doc.Lines.OrderBy(x => x.SkuId))
        {
            // Deterministic SKU lock order prevents last-bag overselling and lost updates.
            var sku = await db.Skus.FromSqlInterpolated($"SELECT * FROM skus WHERE id={line.SkuId} FOR UPDATE").SingleAsync(ct);
            if (!sku.IsActive || !await db.Products.AnyAsync(x => x.Id == sku.ProductId && x.IsActive, ct)) throw new BusinessException(409, "A product or SKU is no longer active.");
            var price = receipt ? line.UnitPriceVnd ?? throw new BusinessException(400, "Receipt cost required.") : sku.SalePriceVnd;
            var quotedLine = quote.Lines.SingleOrDefault(x => x.SkuId == line.SkuId);
            if (quotedLine is null || quotedLine.Quantity != line.Quantity || quotedLine.UnitPriceVnd != price) throw new PriceChangedException();
            total += checked(price * line.Quantity);
            var delta = receipt ? line.Quantity : -line.Quantity;
            // Guard remains in SQL even though the row is locked; the entire document rolls back on failure.
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE skus SET stock_qty=stock_qty+{delta}, stock_version=stock_version+1 WHERE id={sku.Id} AND stock_qty::bigint+{delta} BETWEEN 0 AND 2147483647", ct);
            if (changed != 1) throw new BusinessException(409, $"Insufficient stock or quantity overflow for {sku.Code}. Reload inventory.");
            var balance = checked(sku.StockQty + delta); var stockVersion = sku.StockVersion + 1;
            db.StockMovements.Add(new StockMovement { SkuId = sku.Id, Kind = receipt ? "Purchase" : "Sale", DeltaQty = delta, BalanceAfter = balance, StockVersionAfter = stockVersion, ReceiptId = receipt ? id : null, SaleId = receipt ? null : id, ActorId = actorId });
            if (!receipt)
            {
                var item = await db.SaleLines.SingleAsync(x => x.SaleId == id && x.SkuId == sku.Id, ct); item.UnitPriceVnd = price; item.SkuLabelSnapshot = $"{sku.Code} / {sku.PackageAmount} {sku.PackageUnit}";
            }
            InventoryEvents.Add(db, sku.Id);
        }
        if (total > 9000000000000000m) throw new BusinessException(400, "Document total exceeds the supported whole-VND range.");
        if (total != quote.TotalVnd) throw new PriceChangedException();
        state(total);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (await TradeAsync(receipt, id, ct))!;
    }
    private async Task<Action<decimal>?> LockTradeAsync(bool receipt, int id, ConfirmInput input, int actorId, CancellationToken ct)
    {
        if (receipt)
        {
            var d = await db.PurchaseReceipts.FromSqlInterpolated($"SELECT * FROM purchase_receipts WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Receipt not found.");
            if (d.Status is "Confirmed" or "Cancelled" && d.ConfirmationKey == input.ActionKey)
            {
                ValidateConfirmationReplay(d.Status, d.Version, d.TotalVnd, input);
                return null;
            }
            if (d.Status != "Draft" || d.Version != input.ExpectedVersion) throw new BusinessException(409, "Receipt state changed. Reload.");
            if (!await SupplierActiveAsync(d.SupplierId, ct)) throw new BusinessException(409, "Supplier is no longer active.");
            return total => { d.Status = "Confirmed"; d.ConfirmedBy = actorId; d.ConfirmedAt = DateTime.UtcNow; d.ConfirmationKey = input.ActionKey; d.TotalVnd = total; d.Version++; };
        }
        else
        {
            var d = await db.Sales.FromSqlInterpolated($"SELECT * FROM sales WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Sale not found.");
            if (d.Status is "Confirmed" or "Cancelled" && d.ConfirmationKey == input.ActionKey)
            {
                ValidateConfirmationReplay(d.Status, d.Version, d.TotalVnd, input);
                return null;
            }
            if (d.Status != "Draft" || d.Version != input.ExpectedVersion) throw new BusinessException(409, "Sale state changed. Reload.");
            return total => { d.Status = "Confirmed"; d.ConfirmedBy = actorId; d.ConfirmedAt = DateTime.UtcNow; d.ConfirmationKey = input.ActionKey; d.TotalVnd = total; d.Version++; };
        }
    }
    private static void ValidateConfirmationReplay(string status, long version, decimal? total, ConfirmInput input)
    {
        var originalVersion = version - (status == "Cancelled" ? 2 : 1);
        if (input.ExpectedVersion != originalVersion || input.ExpectedTotalVnd is decimal expected && expected != total)
            throw new BusinessException(409, "Confirmation key was already used with different confirmation values.");
    }
}
