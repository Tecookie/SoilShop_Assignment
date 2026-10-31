using SoilShop.Application;
using SoilShop.Contracts;

namespace SoilShop.Infrastructure;

public static class AdviceProtoMapper
{
    public static AdviceRequest Request(ProductAdviceInput input, IReadOnlyList<ProductAdviceCandidate> candidates)
    {
        var p = new AdvicePreferences { PlantGroup = input.PlantGroup ?? "", Drainage = input.Drainage ?? "", Aeration = input.Aeration ?? "", WaterRetention = input.WaterRetention ?? "", EcMethod = input.EcMethod ?? "", UseContext = input.UseContext ?? "", AllowIncomplete = input.AllowIncomplete, RequireSample = input.RequireSample };
        if (input.PhMin is decimal min) { p.PhMin = checked((int)(min * 100)); p.PhMax = checked((int)(input.PhMax!.Value * 100)); }
        if (input.MaxEc is decimal maxEc) p.MaxEc = checked((int)(maxEc * 1000));
        p.RequiredInformation.AddRange(input.RequiredInformation ?? []);
        var request = new AdviceRequest { Preferences = p };
        foreach (var candidate in candidates)
        {
            var m = candidate.Metadata;
            var item = new AdviceCandidate { SkuId = candidate.Sku.Id, PriceVnd = checked((long)candidate.Sku.SalePriceVnd), StockQty = candidate.Sku.StockQty, IsActive = candidate.Sku.IsActive,
                IsReviewed = m.ReviewedAt is not null && m.ReviewedBy is not null && !string.IsNullOrWhiteSpace(m.SourceReference),
                Drainage = m.Drainage ?? "", Aeration = m.Aeration ?? "", WaterRetention = m.WaterRetention ?? "", EcMethod = m.EcMethod ?? "", UseContext = m.UseContext ?? "" };
            item.PlantGroups.AddRange(m.PlantGroups);
            if (m.PhMin is decimal ph) { item.PhMin = checked((int)(ph * 100)); item.PhMax = checked((int)(m.PhMax!.Value * 100)); }
            if (m.EcMin is decimal ec) { item.EcMin = checked((int)(ec * 1000)); item.EcMax = checked((int)(m.EcMax!.Value * 1000)); }
            if (m.SampleAvailable is bool sample) item.SampleAvailable = sample;
            foreach (var info in new[] { ("Structure", m.StructureNotes), ("Nutrition", m.NutritionNotes), ("Organic", m.OrganicMatterNotes), ("Biology", m.BiologyNotes), ("Cleanliness", m.CleanlinessNotes) })
                if (!string.IsNullOrWhiteSpace(info.Item2)) item.AvailableInformation.Add(info.Item1);
            request.Candidates.Add(item);
        }
        return request;
    }
    public static ProductAdviceRanking Response(AdviceReply reply) => new(reply.RuleVersion, reply.Matches.Select(x => new ProductAdviceRank(x.SkuId, x.Score, x.MaxScore, x.Reasons.ToArray(), x.Missing.ToArray())).ToList());
}
