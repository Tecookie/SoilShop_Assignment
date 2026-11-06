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
