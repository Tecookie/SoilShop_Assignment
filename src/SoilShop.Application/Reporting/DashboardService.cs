namespace SoilShop.Application;

public sealed record DashboardBucket(DateOnly From,DateOnly To,int SaleCount,long SoldBags,decimal SaleVnd,int ReceiptCount,long ReceivedBags,decimal ReceiptVnd);
public sealed record DashboardResult(DateOnly From,DateOnly To,string GroupBy,DateTime GeneratedAt,int LowStockSkus,DashboardBucket Totals,IReadOnlyList<DashboardBucket> Periods);
public interface IDashboardRepository
{
    Task<DashboardResult> GetAsync(DateOnly from,DateOnly to,string groupBy,CancellationToken ct);
}
public sealed class DashboardService(IDashboardRepository repository)
{
    public Task<DashboardResult> GetAsync(DateOnly from,DateOnly to,string groupBy,CancellationToken ct)
    {
        if(from<new DateOnly(2000,1,1) || to>OperationsService.Today || from>to || to.DayNumber-from.DayNumber>365)
            throw new BusinessException(400,"Choose a valid date range up to 366 days ending no later than today (Vietnam time).");
        if(groupBy is not ("day" or "week")) throw new BusinessException(400,"Grouping must be day or week.");
        return repository.GetAsync(from,to,groupBy,ct);
    }
}
