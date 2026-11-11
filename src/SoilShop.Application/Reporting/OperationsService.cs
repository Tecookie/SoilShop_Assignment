using SoilShop.Domain;
namespace SoilShop.Application;

public sealed record SummaryResult(DailySummary Summary,IReadOnlyList<DailySkuSnapshot> Stock);
public interface IOperationsRepository
{
    Task<PageResult<DailySummary>> SummariesAsync(int page,int size,CancellationToken ct);
    Task<SummaryResult> SummaryAsync(DateOnly date,CancellationToken ct);
    Task<DailySummary> QueueSummaryAsync(DateOnly date,CancellationToken ct);
    Task<PageResult<BackupRun>> BackupsAsync(int page,int size,CancellationToken ct);
    Task<BackupRun> QueueBackupAsync(Guid key,int actor,CancellationToken ct);
}
public sealed class OperationsService(IOperationsRepository repository)
{
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")));
    public Task<PageResult<DailySummary>> SummariesAsync(int page,int size,CancellationToken ct) { Pagination.Validate(page,size); return repository.SummariesAsync(page,size,ct); }
    public Task<SummaryResult> SummaryAsync(DateOnly date,CancellationToken ct)=>repository.SummaryAsync(date,ct);
    public Task<DailySummary> QueueSummaryAsync(DateOnly date,CancellationToken ct)
    {
        if(date>Today || date<new DateOnly(2000,1,1)) throw new BusinessException(400,"Select a business date from 2000 through today (Vietnam time).");
        return repository.QueueSummaryAsync(date,ct);
    }
    public Task<PageResult<BackupRun>> BackupsAsync(int page,int size,CancellationToken ct) { Pagination.Validate(page,size); return repository.BackupsAsync(page,size,ct); }
    public Task<BackupRun> QueueBackupAsync(Guid key,int actor,CancellationToken ct)
    {
        if(key==Guid.Empty) throw new BusinessException(400,"Request key is required.");
        return repository.QueueBackupAsync(key,actor,ct);
    }
}
