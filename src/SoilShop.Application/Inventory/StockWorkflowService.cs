using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;
namespace SoilShop.Application;

public sealed record CountInput([Range(1,int.MaxValue)] int SkuId, [Range(0,int.MaxValue)] int ExpectedQty,
    [Range(0,long.MaxValue)] long ExpectedStockVersion, [Range(0,1000000)] int ActualQty, [Required,MaxLength(500)] string Reason);
public sealed record CountReviewInput([Range(1,long.MaxValue)] long ExpectedVersion, bool Approve, [Required,MaxLength(500)] string Note);
public sealed record AdjustmentInput(Guid RequestKey, [Range(1,int.MaxValue)] int SkuId, int DeltaQty,
    [Range(0,long.MaxValue)] long ExpectedStockVersion, [Required,MaxLength(500)] string Reason);
public sealed record ConvertSuggestionInput([Range(1,long.MaxValue)] long ExpectedVersion, [Range(1,int.MaxValue)] int SupplierId);
public interface IStockWorkflowRepository
{
    Task<PageResult<StockCount>> CountsAsync(int page, int size, CancellationToken ct);
    Task<StockCount> CountAsync(CountInput input, int actor, CancellationToken ct);
    Task<StockCount> ReviewAsync(int id, CountReviewInput input, int actor, CancellationToken ct);
    Task<StockAdjustment> AdjustAsync(AdjustmentInput input, int actor, CancellationToken ct);
    Task<int> ConvertAsync(int id, ConvertSuggestionInput input, int actor, CancellationToken ct);
}
public sealed class StockWorkflowService(IStockWorkflowRepository repository)
{
    public static string Reason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 500) throw new BusinessException(400,"A reason of 1–500 characters is required.");
        return value.Trim();
    }
    public Task<PageResult<StockCount>> CountsAsync(int page,int size,CancellationToken ct) { Pagination.Validate(page,size); return repository.CountsAsync(page,size,ct); }
    public Task<StockCount> CountAsync(CountInput input,int actor,CancellationToken ct) => repository.CountAsync(input with { Reason=Reason(input.Reason) },actor,ct);
    public Task<StockCount> ReviewAsync(int id,CountReviewInput input,int actor,CancellationToken ct) => repository.ReviewAsync(id,input with { Note=Reason(input.Note) },actor,ct);
    public Task<StockAdjustment> AdjustAsync(AdjustmentInput input,int actor,CancellationToken ct)
    {
        if(input.RequestKey==Guid.Empty || input.DeltaQty is 0 or < -1000000 or > 1000000) throw new BusinessException(400,"Request key and a nonzero change within ±1,000,000 bags are required.");
        return repository.AdjustAsync(input with { Reason=Reason(input.Reason) },actor,ct);
    }
    public Task<int> ConvertAsync(int id,ConvertSuggestionInput input,int actor,CancellationToken ct) => repository.ConvertAsync(id,input,actor,ct);
}
