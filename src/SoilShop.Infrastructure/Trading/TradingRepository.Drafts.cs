using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed partial class TradingRepository
{
    private async Task<Action> LockDraftAsync(bool receipt, int id, long expectedVersion, CancellationToken ct)
    {
        if (receipt)
        {
            var doc = await db.PurchaseReceipts.FromSqlInterpolated($"SELECT * FROM purchase_receipts WHERE id={id} FOR UPDATE")
                .SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Receipt not found.");
            if (doc.Status != "Draft" || doc.Version != expectedVersion) throw new BusinessException(409, "Only the current Draft can be changed. Reload.");
            return () => doc.Version++;
        }
        var sale = await db.Sales.FromSqlInterpolated($"SELECT * FROM sales WHERE id={id} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Sale not found.");
        if (sale.Status != "Draft" || sale.Version != expectedVersion) throw new BusinessException(409, "Only the current Draft can be changed. Reload.");
        return () => sale.Version++;
    }

    public async Task<TradeDto> UpdateTradeAsync(bool receipt, int id, TradeInput input, long expectedVersion, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var advance = await LockDraftAsync(receipt, id, expectedVersion, ct);
        var ids = input.Lines.Select(x => x.SkuId).ToArray();
        if (await db.Skus.CountAsync(x => ids.Contains(x.Id) && x.IsActive && db.Products.Any(p => p.Id == x.ProductId && p.IsActive), ct) != ids.Length)
            throw new BusinessException(400, "Some SKUs are unavailable.");
        if (receipt)
        {
            if (!await SupplierActiveAsync(input.SupplierId!.Value, ct)) throw new BusinessException(409, "Supplier is no longer active.");
            var doc = await db.PurchaseReceipts.SingleAsync(x => x.Id == id, ct);
            doc.SupplierId = input.SupplierId.Value;
            await db.PurchaseReceiptLines.Where(x => x.ReceiptId == id).ExecuteDeleteAsync(ct);
            db.PurchaseReceiptLines.AddRange(input.Lines.Select(x => new PurchaseReceiptLine
                { ReceiptId = id, SkuId = x.SkuId, Quantity = x.Quantity, UnitCostVnd = x.UnitCostVnd }));
        }
        else
        {
            await db.SaleLines.Where(x => x.SaleId == id).ExecuteDeleteAsync(ct);
            db.SaleLines.AddRange(input.Lines.Select(x => new SaleLine { SaleId = id, SkuId = x.SkuId, Quantity = x.Quantity }));
        }
        advance();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await TradeAsync(receipt, id, ct))!;
    }

    public async Task DeleteTradeAsync(bool receipt, int id, long expectedVersion, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockDraftAsync(receipt, id, expectedVersion, ct);
        if (receipt)
        {
            // Clear the FK and reset its state atomically before deleting a linked draft.
            await db.RestockSuggestions.Where(x => x.ReceiptId == id).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ReceiptId, (int?)null).SetProperty(x => x.Status, "Open")
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow).SetProperty(x => x.Version, x => x.Version + 1), ct);
            await db.PurchaseReceipts.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        }
        else await db.Sales.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);
    }

}
