using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed partial class ShopRepository
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

    public async Task<TradeDto> CancelTradeAsync(bool receipt, int id, CancelTradeInput input, int actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Locking the document serializes different keys as well as repeated requests.
        PurchaseReceipt? purchase = null;
        Sale? sale = null;
        if (receipt) purchase = await db.PurchaseReceipts.FromSqlInterpolated($"SELECT * FROM purchase_receipts WHERE id={id} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Receipt not found.");
        else sale = await db.Sales.FromSqlInterpolated($"SELECT * FROM sales WHERE id={id} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Sale not found.");
        var status = purchase?.Status ?? sale!.Status;
        var previousKey = purchase?.CancellationKey ?? sale?.CancellationKey;
        var previousReason = purchase?.CancelReason ?? sale?.CancelReason;
        if (status == "Cancelled" && previousKey == input.ActionKey)
        {
            if (previousReason != input.Reason || input.ExpectedVersion != (purchase?.Version ?? sale!.Version) - 1)
                throw new BusinessException(409, "Cancellation key was already used with different cancellation values.");
            await tx.CommitAsync(ct);
            return (await TradeAsync(receipt, id, ct))!;
        }
        if (status != "Confirmed" || (purchase?.Version ?? sale!.Version) != input.ExpectedVersion)
            throw new BusinessException(409, "Only the current Confirmed document can be cancelled. Reload.");
        var document = (await TradeAsync(receipt, id, ct))!;
        foreach (var line in document.Lines.OrderBy(x => x.SkuId))
        {
            // Reversals deliberately allow deactivated master data.
            var sku = await db.Skus.FromSqlInterpolated($"SELECT * FROM skus WHERE id={line.SkuId} FOR UPDATE").SingleAsync(ct);
            var delta = receipt ? -line.Quantity : line.Quantity;
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE skus SET stock_qty=stock_qty+{delta}, stock_version=stock_version+1 WHERE id={sku.Id} AND stock_qty::bigint+{delta} BETWEEN 0 AND 2147483647", ct);
            if (changed != 1) throw new BusinessException(409, $"Cannot reverse {sku.Code}: insufficient stock or quantity overflow. No lines were cancelled.");
            db.StockMovements.Add(new StockMovement
            {
                SkuId = sku.Id, Kind = receipt ? "PurchaseCancel" : "SaleCancel", DeltaQty = delta,
                BalanceAfter = checked(sku.StockQty + delta), StockVersionAfter = sku.StockVersion + 1,
                ReceiptId = receipt ? id : null, SaleId = receipt ? null : id, ActorId = actorId
            });
            AddInventoryEvent(sku.Id);
        }
        if (purchase is not null)
        {
            purchase.Status = "Cancelled"; purchase.CancelledBy = actorId; purchase.CancelledAt = DateTime.UtcNow;
            purchase.CancellationKey = input.ActionKey; purchase.CancelReason = input.Reason; purchase.Version++;
        }
        else
        {
            sale!.Status = "Cancelled"; sale.CancelledBy = actorId; sale.CancelledAt = DateTime.UtcNow;
            sale.CancellationKey = input.ActionKey; sale.CancelReason = input.Reason; sale.Version++;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await TradeAsync(receipt, id, ct))!;
    }
}
