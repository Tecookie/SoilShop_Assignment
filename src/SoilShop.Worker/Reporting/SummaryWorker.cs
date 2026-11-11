using Microsoft.EntityFrameworkCore;
using SoilShop.Infrastructure;
using SoilShop.Application;
namespace SoilShop.Worker;

public sealed class SummaryWorker(IServiceScopeFactory scopes,WorkerSettings settings,ILogger<SummaryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope=scopes.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
                if(VietnamSchedule.SummaryDate(DateTime.UtcNow) is DateOnly date)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO daily_summaries (business_date) VALUES ({date}) ON CONFLICT (business_date) DO NOTHING",ct);
                }
                var queued=await db.DailySummaries.AsNoTracking().Where(x=>x.Status=="Queued").OrderBy(x=>x.BusinessDate).Select(x=>(int?)x.Id).FirstOrDefaultAsync(ct);
                try { await new SummaryProcessor(db).ProcessNextAsync(ct); }
                catch(Exception ex) when(ex is not OperationCanceledException)
                {
                    db.ChangeTracker.Clear();
                    if(queued.HasValue) await db.DailySummaries.Where(x=>x.Id==queued && x.Status=="Queued").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Failed").SetProperty(x=>x.LastError,"Summary failed; previous snapshot preserved. Request a refresh.").SetProperty(x=>x.Version,x=>x.Version+1),ct);
                    logger.LogWarning("Summary failed: {ErrorType}",ex.GetType().Name);
                }
            }
            catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogWarning("Summary worker will retry: {ErrorType}",ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(settings.JobPollSeconds),ct); } catch(OperationCanceledException) { break; }
        }
    }
}
