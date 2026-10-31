using Grpc.Core;
using SoilShop.Contracts;

namespace SoilShop.CalculationHost;

public sealed class TotalsService(ILogger<TotalsService> logger) : Calculation.CalculationBase
{
    public override Task<AdviceReply> RecommendProducts(AdviceRequest request, ServerCallContext context)
    {
        var result = AdviceRules.Rank(request);
        logger.LogInformation("Ranked {CandidateCount} candidates; returned {MatchCount} using {RuleVersion}", request.Candidates.Count, result.Matches.Count, result.RuleVersion);
        return Task.FromResult(result);
    }
    public static long Calculate(TotalsRequest request)
    {
        if (request.Lines.Count is < 1 or > 100) throw new RpcException(new Status(StatusCode.InvalidArgument, "Use 1–100 lines."));
        try
        {
            long total = 0;
            foreach (var line in request.Lines)
            {
                if (line.Quantity is < 1 or > 1000000 || line.UnitPriceVnd is < 1 or > 1000000000000) throw new RpcException(new Status(StatusCode.InvalidArgument, "Positive bag quantities and whole VND required."));
                total = checked(total + checked(line.Quantity * line.UnitPriceVnd));
            }
            // Whole VND remains exact in the browser's integer number range.
            if (total > 9000000000000000) throw new OverflowException();
            return total;
        }
        catch (OverflowException) { throw new RpcException(new Status(StatusCode.InvalidArgument, "Document total exceeds its supported range.")); }
    }
    public override Task<TotalsReply> CalculateTotals(TotalsRequest request, ServerCallContext context)
    {
        var total = Calculate(request); logger.LogInformation("Calculated {LineCount} lines using totals-v1", request.Lines.Count);
        return Task.FromResult(new TotalsReply { TotalVnd = total, RuleVersion = "totals-v1" });
    }
}
