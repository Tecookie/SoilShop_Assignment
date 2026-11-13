using Microsoft.EntityFrameworkCore;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class SummaryProcessor(ShopDbContext db)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);
        var summary=await db.DailySummaries.FromSqlRaw("SELECT * FROM daily_summaries WHERE status='Queued' ORDER BY business_date LIMIT 1 FOR UPDATE SKIP LOCKED").FirstOrDefaultAsync(ct);
        if(summary is null) return false;
        var zone=TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var from=TimeZoneInfo.ConvertTimeToUtc(summary.BusinessDate.ToDateTime(TimeOnly.MinValue),zone);
        var until=TimeZoneInfo.ConvertTimeToUtc(summary.BusinessDate.AddDays(1).ToDateTime(TimeOnly.MinValue),zone);
        var sales=db.Sales.Where(x=>x.Status=="Confirmed" && x.ConfirmedAt>=from && x.ConfirmedAt<until);
        summary.RevenueVnd=await sales.SumAsync(x=>x.TotalVnd ?? 0,ct);
        summary.SaleCount=await sales.CountAsync(ct);
        summary.SoldBags=await db.SaleLines.Where(x=>sales.Any(s=>s.Id==x.SaleId)).SumAsync(x=>(long)x.Quantity,ct);
        await db.DailySkuSnapshots.Where(x=>x.SummaryId==summary.Id).ExecuteDeleteAsync(ct);
        db.DailySkuSnapshots.AddRange(await db.Skus.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new DailySkuSnapshot { SummaryId=summary.Id,SkuId=x.Id,StockQty=x.StockQty }).ToListAsync(ct));
        summary.Status="Completed"; summary.SnapshotAt=DateTime.UtcNow; summary.LastError=null; summary.Version++;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }
}
