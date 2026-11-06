using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record ProductAdviceInput(string? PlantGroup = null, string? Drainage = null, string? Aeration = null,
    string? WaterRetention = null, decimal? PhMin = null, decimal? PhMax = null, decimal? MaxEc = null,
    string? EcMethod = null, string? UseContext = null, bool AllowIncomplete = false,
    List<string>? RequiredInformation = null, bool RequireSample = false,
    [Range(typeof(decimal), "1", "1000000000000")] decimal? MaxPriceVnd = null,
    string? PackageUnit = null, decimal? PackageAmount = null);
public sealed record ProductAdviceCandidate(SkuDto Sku, string ProductName, SoilMetadataDto Metadata);
public sealed record ProductAdviceRank(int SkuId, int Score, int MaxScore, IReadOnlyList<string> Reasons, IReadOnlyList<string> Missing);
public sealed record ProductAdviceRanking(string RuleVersion, IReadOnlyList<ProductAdviceRank> Matches);
public sealed record ProductAdviceResult(ProductAdviceCandidate Candidate, int Score, int MaxScore, IReadOnlyList<string> Reasons, IReadOnlyList<string> Missing);
public sealed record ProductAdviceResponse(DateTime GeneratedAt, string RuleVersion, int CandidatesConsidered, IReadOnlyList<ProductAdviceResult> Results, string Message);
public interface IProductAdviceClient
{
    Task<ProductAdviceRanking> RankAsync(ProductAdviceInput input, IReadOnlyList<ProductAdviceCandidate> candidates, CancellationToken ct);
}
public interface IAdviceCandidateRepository
{
    Task<IReadOnlyList<ProductAdviceCandidate>> CandidatesAsync(ProductAdviceInput input, CancellationToken ct);
}
public sealed class ProductAdviceService(IAdviceCandidateRepository repository, IProductAdviceClient client)
{
    public async Task<ProductAdviceResponse> RecommendAsync(ProductAdviceInput input, CancellationToken ct)
    {
        if (input.PlantGroup is not null && !SoilOptions.PlantGroups.ContainsKey(input.PlantGroup)) throw new BusinessException(400, "Unknown plant group.");
        SoilMetadataService.Option(input.Drainage, SoilOptions.Levels, "drainage"); SoilMetadataService.Option(input.Aeration, SoilOptions.Levels, "aeration");
        SoilMetadataService.Option(input.WaterRetention, SoilOptions.Levels, "retention"); SoilMetadataService.Option(input.UseContext, SoilOptions.Contexts, "context");
        SoilMetadataService.Range(input.PhMin, input.PhMax, 14, 2, "pH");
        if (input.MaxEc is decimal ec && (ec < 0 || ec > 99999.999m || decimal.Round(ec, 3) != ec || input.EcMethod is not ("SME" or "Extract1to2" or "PourThrough"))) throw new BusinessException(400, "EC needs a nonnegative maximum in mS/cm and a comparable SME, 1:2 or PourThrough method.");
        if (input.MaxEc is null && input.EcMethod is not null) throw new BusinessException(400, "Supply an EC maximum with the measurement method.");
        if (input.PackageUnit is not null && input.PackageUnit is not ("kg" or "L")) throw new BusinessException(400, "Package unit must be kg or L.");
        if (input.PackageAmount is decimal amount && (amount <= 0 || amount > 1000000 || decimal.Round(amount, 2) != amount || input.PackageUnit is null)) throw new BusinessException(400, "Package amount needs a unit and at most two decimals.");
        if (input.MaxPriceVnd is decimal money && (money <= 0 || money > 1000000000000m || decimal.Truncate(money) != money)) throw new BusinessException(400, "Budget is positive whole VND per bag.");
        if (input.RequiredInformation is { } info && (info.Count > 5 || info.Distinct().Count() != info.Count || info.Any(x => x is not ("Structure" or "Nutrition" or "Organic" or "Biology" or "Cleanliness")))) throw new BusinessException(400, "Choose supported information-presence filters.");
        if (input.PlantGroup is null && input.Drainage is null && input.Aeration is null && input.WaterRetention is null && input.PhMin is null && input.MaxEc is null && input.UseContext is null) throw new BusinessException(400, "Choose at least one plant, physical, pH, EC or planting-context preference.");
        var candidates = await repository.CandidatesAsync(input, ct);
        if (candidates.Count > 500) throw new BusinessException(400, "More than 500 candidates; narrow the package or budget filters.");
        var ranking = await client.RankAsync(input, candidates, ct);
        var byId = candidates.ToDictionary(x => x.Sku.Id);
        var results = ranking.Matches.Select(x => new ProductAdviceResult(byId[x.SkuId], x.Score, x.MaxScore, x.Reasons, x.Missing)).ToList();
        return new(DateTime.UtcNow, ranking.RuleVersion, candidates.Count, results,
            results.Count == 0 ? candidates.Count == 0
                ? "Không có SKU đang bán, còn tồn và có thông tin nguồn đã duyệt trong bộ lọc này. Admin vào Catalogue → Thông số đất, chọn nhóm cây theo nguồn, Lưu rồi Duyệt. Loại mặt hàng Đa dụng không tự tạo nhóm cây tư vấn."
                : "Có SKU đủ điều kiện nhưng chưa khớp tiêu chí. Kiểm tra nhóm cây trong thông số đã duyệt, bớt điều kiện hoặc cho phép thiếu thông tin. Loại mặt hàng khác với nhóm cây tư vấn."
                : "Kết quả khớp tiêu chí đã nhập, không phải chứng nhận chất lượng hay bảo đảm cây phát triển. Thông tin thiếu được ghi rõ; phiếu bán sẽ kiểm tra lại giá và tồn.");
    }
}
