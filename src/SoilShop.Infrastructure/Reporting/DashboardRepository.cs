using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
namespace SoilShop.Infrastructure;

public sealed class DashboardRepository(ShopDbContext db) : IDashboardRepository
{
    public async Task<DashboardResult> GetAsync(DateOnly from,DateOnly to,string groupBy,CancellationToken ct)
    {
        var zone=TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var start=TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue),zone);
        var end=TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue),zone);
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);
        var sales=await db.Sales.AsNoTracking().Where(x=>x.Status=="Confirmed" && x.ConfirmedAt>=start && x.ConfirmedAt<end)
            .Select(x=>new {At=x.ConfirmedAt!.Value,Amount=x.TotalVnd ?? 0,Bags=db.SaleLines.Where(l=>l.SaleId==x.Id).Sum(l=>(long?)l.Quantity) ?? 0}).ToListAsync(ct);
        var receipts=await db.PurchaseReceipts.AsNoTracking().Where(x=>x.Status=="Confirmed" && x.ConfirmedAt>=start && x.ConfirmedAt<end)
            .Select(x=>new {At=x.ConfirmedAt!.Value,Amount=x.TotalVnd ?? 0,Bags=db.PurchaseReceiptLines.Where(l=>l.ReceiptId==x.Id).Sum(l=>(long?)l.Quantity) ?? 0}).ToListAsync(ct);
        var low=await db.Skus.CountAsync(x=>x.IsActive && x.StockQty<=x.LowStockThreshold && db.Products.Any(p=>p.Id==x.ProductId && p.IsActive),ct);
        DateOnly Day(DateTime utc)=>DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc,zone));
        var salesByDay=sales.GroupBy(x=>Day(x.At)).ToDictionary(x=>x.Key,x=>(Count:x.Count(),Bags:x.Sum(v=>v.Bags),Amount:x.Sum(v=>v.Amount)));
        var receiptsByDay=receipts.GroupBy(x=>Day(x.At)).ToDictionary(x=>x.Key,x=>(Count:x.Count(),Bags:x.Sum(v=>v.Bags),Amount:x.Sum(v=>v.Amount)));
        var buckets=new List<DashboardBucket>();
        for(var current=from;current<=to;)
        {
            var last=groupBy=="day"?current:current.AddDays(6-(((int)current.DayOfWeek+6)%7));
            if(last>to) last=to;
            int sc=0,rc=0;long sb=0,rb=0;decimal sv=0,rv=0;
            for(var date=current;date<=last;date=date.AddDays(1))
            {
                if(salesByDay.TryGetValue(date,out var s)) { sc+=s.Count;sb+=s.Bags;sv+=s.Amount; }
                if(receiptsByDay.TryGetValue(date,out var r)) { rc+=r.Count;rb+=r.Bags;rv+=r.Amount; }
            }
            buckets.Add(new(current,last,sc,sb,sv,rc,rb,rv));current=last.AddDays(1);
        }
        var totals=new DashboardBucket(from,to,buckets.Sum(x=>x.SaleCount),buckets.Sum(x=>x.SoldBags),buckets.Sum(x=>x.SaleVnd),buckets.Sum(x=>x.ReceiptCount),buckets.Sum(x=>x.ReceivedBags),buckets.Sum(x=>x.ReceiptVnd));
        var result=new DashboardResult(from,to,groupBy,DateTime.UtcNow,low,totals,buckets);
        await tx.CommitAsync(ct);return result;
    }
}
