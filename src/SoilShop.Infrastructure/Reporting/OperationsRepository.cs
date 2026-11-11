using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class OperationsRepository(ShopDbContext db) : IOperationsRepository
{
    public async Task<PageResult<DailySummary>> SummariesAsync(int page,int size,CancellationToken ct)=>new(await db.DailySummaries.AsNoTracking().OrderByDescending(x=>x.BusinessDate).Skip((page-1)*size).Take(size).ToListAsync(ct),await db.DailySummaries.CountAsync(ct),page,size);
    public async Task<SummaryResult> SummaryAsync(DateOnly date,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);
        var summary=await db.DailySummaries.AsNoTracking().SingleOrDefaultAsync(x=>x.BusinessDate==date,ct) ?? throw new BusinessException(404,"Summary not found.");
        var stock=await db.DailySkuSnapshots.AsNoTracking().Where(x=>x.SummaryId==summary.Id).OrderBy(x=>x.SkuId).ToListAsync(ct);
        await tx.CommitAsync(ct); return new(summary,stock);
    }
    public async Task<DailySummary> QueueSummaryAsync(DateOnly date,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO daily_summaries (business_date) VALUES ({date}) ON CONFLICT (business_date) DO UPDATE SET status='Queued',last_error=NULL,version=daily_summaries.version+1",ct);
        db.OutboxMessages.Add(new OutboxMessage { Id=Guid.NewGuid(),Kind="SummaryRequested",Payload=System.Text.Json.JsonSerializer.Serialize(new { businessDate=date }) });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await db.DailySummaries.AsNoTracking().SingleAsync(x=>x.BusinessDate==date,ct);
    }
    public async Task<PageResult<BackupRun>> BackupsAsync(int page,int size,CancellationToken ct)=>new(await db.BackupRuns.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync(ct),await db.BackupRuns.CountAsync(ct),page,size);
    public async Task<BackupRun> QueueBackupAsync(Guid key,int actor,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(23205)",ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key.ToString()},0))",ct);
        var existing=await db.BackupRuns.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==key,ct);
        if(existing is not null)
        {
            if(existing.Kind!="Manual" || existing.RequestedBy!=actor) throw new BusinessException(409,"Request key already used for another backup.");
            await tx.CommitAsync(ct); return existing;
        }
        if(await db.BackupRuns.AnyAsync(x=>x.Kind=="Manual" && (x.Status=="Queued" || x.Status=="Processing"),ct)) throw new BusinessException(409,"A manual backup is already pending. Wait for its result.");
        var run=new BackupRun { Id=key,Kind="Manual",RequestedBy=actor };
        db.BackupRuns.Add(run); db.OutboxMessages.Add(new OutboxMessage { Id=Guid.NewGuid(),Kind="BackupRequested",Payload=System.Text.Json.JsonSerializer.Serialize(new { backupId=key }) });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return run;
    }
}
