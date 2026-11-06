using Grpc.Core;
using SoilShop.Contracts;

namespace SoilShop.CalculationHost;

public sealed class CalculationGrpcService(ILogger<CalculationGrpcService> logger) : Calculation.CalculationBase
{
    public override Task<AdviceReply> RecommendProducts(AdviceRequest request, ServerCallContext context)
    {
        var result = AdviceRules.Rank(request);
        logger.LogInformation("Ranked {CandidateCount} candidates; returned {MatchCount} using {RuleVersion}", request.Candidates.Count, result.Matches.Count, result.RuleVersion);
        return Task.FromResult(result);
    }
    public override Task<TotalsReply> CalculateTotals(TotalsRequest request, ServerCallContext context)
    {
        var total = TotalsRules.Calculate(request); logger.LogInformation("Calculated {LineCount} lines using totals-v1", request.Lines.Count);
        return Task.FromResult(new TotalsReply { TotalVnd = total, RuleVersion = "totals-v1" });
    }
}
