using SoilShop.CalculationHost;
using SoilShop.Contracts;
using Xunit;

namespace SoilShop.Tests;
public sealed class AdviceRuleTests
{
    private static AdviceCandidate Candidate(int id, string aeration = "High", int price = 50000)
    {
        var item = new AdviceCandidate { SkuId = id, PriceVnd = price, StockQty = 5, IsActive = true, IsReviewed = true, Drainage = "High", Aeration = aeration, WaterRetention = "Medium" };
        item.PlantGroups.Add("Foliage"); return item;
    }
    [Fact] public void Physical_matches_rank_above_unknowns_and_reasons_are_returned()
    {
        var request = new AdviceRequest { Preferences = new() { Aeration = "High", AllowIncomplete = true } };
        request.Candidates.AddRange([Candidate(2, "", 10000), Candidate(1), Candidate(3, "Low")]);
        var result = AdviceRules.Rank(request);
        Assert.Single(result.Matches); Assert.Equal(1, result.Matches[0].SkuId);
        Assert.Equal(3, result.Matches[0].Score); Assert.NotEmpty(result.Matches[0].Reasons);
    }
    [Fact] public void Unknowns_are_not_matches_and_strict_mode_excludes_them()
    {
        var request = new AdviceRequest { Preferences = new() { Aeration = "High", PlantGroup = "Foliage" } };
        request.Candidates.Add(Candidate(1, "")); Assert.Empty(AdviceRules.Rank(request).Matches);
        request.Preferences.AllowIncomplete = true;
        var result = AdviceRules.Rank(request); Assert.Single(result.Matches);
        Assert.Equal(3, result.Matches[0].Score); Assert.Equal(6, result.Matches[0].MaxScore); Assert.NotEmpty(result.Matches[0].Missing);
    }
    [Fact] public void Ec_requires_comparable_method_and_ph_must_fit_entire_requested_interval()
    {
        var request = new AdviceRequest { Preferences = new() { MaxEc = 1000, EcMethod = "SME", PhMin = 500, PhMax = 650 } };
        var a = Candidate(1); a.EcMin = 100; a.EcMax = 500; a.EcMethod = "PourThrough"; a.PhMin = 550; a.PhMax = 600;
        request.Candidates.Add(a); Assert.Empty(AdviceRules.Rank(request).Matches);
        a.EcMethod = "SME"; Assert.Single(AdviceRules.Rank(request).Matches);
        a.PhMax = 700; Assert.Empty(AdviceRules.Rank(request).Matches);
    }
    [Fact] public void Only_reviewed_available_positive_matches_return_and_ties_use_price_then_id()
    {
        var request = new AdviceRequest { Preferences = new() { Drainage = "High" } };
        var unreviewed = Candidate(7); unreviewed.IsReviewed = false;
        var empty = Candidate(8); empty.StockQty = 0;
        request.Candidates.AddRange([Candidate(4, price: 70000), Candidate(3), Candidate(2), Candidate(1), unreviewed, empty]);
        Assert.Equal(new[] { 1, 2, 3 }, AdviceRules.Rank(request).Matches.Select(x => x.SkuId));
    }
}
