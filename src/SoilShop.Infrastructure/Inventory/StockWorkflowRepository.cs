using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class StockWorkflowRepository(ShopDbContext db) : IStockWorkflowRepository
{
    public async Task<PageResult<StockCount>> CountsAsync(int page,int size,CancellationToken ct) => new(
        await db.StockCounts.AsNoTracking().OrderByDescending(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync(ct),await db.StockCounts.CountAsync(ct),page,size);
    private async Task<Sku> LockSku(int id,CancellationToken ct) => await db.Skus.FromSqlInterpolated($"SELECT * FROM skus WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw new BusinessException(404,"SKU not found.");
    public async Task<StockCount> CountAsync(CountInput input,int actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var sku=await LockSku(input.SkuId,ct);
        if(sku.StockVersion!=input.ExpectedStockVersion || sku.StockQty!=input.ExpectedQty) throw new BusinessException(409,"Stock changed while counting. Reload and recount.");
        var count=new StockCount { SkuId=sku.Id,ExpectedQty=sku.StockQty,ExpectedStockVersion=sku.StockVersion,ActualQty=input.ActualQty,Reason=input.Reason,CountedBy=actor };
        db.StockCounts.Add(count); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return count;
    }
    public async Task<StockCount> ReviewAsync(int id,CountReviewInput input,int actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var count=await db.StockCounts.FromSqlInterpolated($"SELECT * FROM stock_counts WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw new BusinessException(404,"Count not found.");
        if(count.Status!="Pending" || count.Version!=input.ExpectedVersion) throw new BusinessException(409,"Count was already reviewed. Reload.");
        var sku=await LockSku(count.SkuId,ct);
        count.Status=!input.Approve ? "Rejected" : sku.StockVersion!=count.ExpectedStockVersion || sku.StockQty!=count.ExpectedQty ? "RecountRequired" : "Approved";
        count.ReviewedBy=actor; count.ReviewedAt=DateTime.UtcNow; count.ReviewNote=input.Note; count.Version++;
        if(count.Status=="Approved" && count.ActualQty!=count.ExpectedQty)
            await Apply(sku,count.ActualQty-count.ExpectedQty,count.Reason,actor,Guid.NewGuid(),count.Id,ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return count;
    }
    public async Task<StockAdjustment> AdjustAsync(AdjustmentInput input,int actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        // Serialize identical keys even across SKUs; payload mismatch cannot create another effect.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({input.RequestKey.ToString()},0))",ct);
        var existing=await db.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestKey==input.RequestKey,ct);
        if(existing is not null)
        {
            if(existing.SkuId!=input.SkuId || existing.DeltaQty!=input.DeltaQty || existing.Reason!=input.Reason || existing.CreatedBy!=actor || existing.StockCountId!=null)
                throw new BusinessException(409,"Request key already used for another adjustment.");
            var movement=await db.StockMovements.SingleAsync(x=>x.AdjustmentId==existing.Id,ct);
            if(movement.StockVersionAfter-1!=input.ExpectedStockVersion) throw new BusinessException(409,"Request key already used with another stock version.");
            await tx.CommitAsync(ct); return existing;
        }
        var sku=await LockSku(input.SkuId,ct);
        if(sku.StockVersion!=input.ExpectedStockVersion) throw new BusinessException(409,"Stock changed. Reload before adjusting.");
        var result=await Apply(sku,input.DeltaQty,input.Reason,actor,input.RequestKey,null,ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
    }
    private async Task<StockAdjustment> Apply(Sku sku,int delta,string reason,int actor,Guid key,int? countId,CancellationToken ct)
    {
        var balance=(long)sku.StockQty+delta;
        if(balance<0 || balance>int.MaxValue) throw new BusinessException(409,"Adjustment would exceed supported stock or make it negative.");
        sku.StockQty=(int)balance; sku.StockVersion++;
        var adjustment=new StockAdjustment { RequestKey=key,SkuId=sku.Id,DeltaQty=delta,Reason=reason,CreatedBy=actor,StockCountId=countId };
        db.StockAdjustments.Add(adjustment); await db.SaveChangesAsync(ct);
        db.StockMovements.Add(new StockMovement { SkuId=sku.Id,Kind="Adjustment",DeltaQty=delta,BalanceAfter=sku.StockQty,StockVersionAfter=sku.StockVersion,AdjustmentId=adjustment.Id,ActorId=actor });
        InventoryEvents.Add(db,sku.Id); return adjustment;
    }
    public async Task<int> ConvertAsync(int id,ConvertSuggestionInput input,int actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var suggestion=await db.RestockSuggestions.FromSqlInterpolated($"SELECT * FROM restock_suggestions WHERE id={id} FOR UPDATE").SingleOrDefaultAsync(ct) ?? throw new BusinessException(404,"Suggestion not found.");
        if(suggestion.Status=="ReceiptDrafted" && suggestion.ReceiptId is int linked)
        {
            var receipt=await db.PurchaseReceipts.AsNoTracking().SingleAsync(x=>x.Id==linked,ct);
            if(receipt.SupplierId!=input.SupplierId) throw new BusinessException(409,"Suggestion already converted with a different supplier.");
            await tx.CommitAsync(ct); return linked;
        }
        if(suggestion.Status!="Open" || suggestion.Version!=input.ExpectedVersion) throw new BusinessException(409,"Suggestion changed. Reload.");
        var sku=await LockSku(suggestion.SkuId,ct);
        if(!sku.IsActive || !await db.Products.AnyAsync(x=>x.Id==sku.ProductId && x.IsActive,ct) || sku.StockQty>sku.LowStockThreshold) throw new BusinessException(409,"SKU is no longer eligible for replenishment.");
        if(!await db.Suppliers.AnyAsync(x=>x.Id==input.SupplierId && x.IsActive,ct)) throw new BusinessException(400,"Select an active supplier.");
        var quantity=sku.TargetStock-sku.StockQty;
        if(quantity is <1 or >1000000) throw new BusinessException(400,"Suggested quantity exceeds receipt line limits.");
        var doc=new PurchaseReceipt { Code="IN-"+Guid.NewGuid().ToString("N"),SupplierId=input.SupplierId,CreatedBy=actor };
        db.PurchaseReceipts.Add(doc); await db.SaveChangesAsync(ct);
        db.PurchaseReceiptLines.Add(new PurchaseReceiptLine { ReceiptId=doc.Id,SkuId=sku.Id,Quantity=quantity,UnitCostVnd=sku.ReferenceCostVnd });
        suggestion.ReceiptId=doc.Id; suggestion.SupplierId=input.SupplierId; suggestion.Status="ReceiptDrafted"; suggestion.Version++; suggestion.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return doc.Id;
    }
}
