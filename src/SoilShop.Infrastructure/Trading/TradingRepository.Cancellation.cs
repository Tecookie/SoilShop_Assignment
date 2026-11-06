using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed partial class TradingRepository
{
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
            InventoryEvents.Add(db, sku.Id);
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
