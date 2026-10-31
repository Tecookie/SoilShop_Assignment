using Grpc.Core;
using SoilShop.Contracts;

namespace SoilShop.CalculationHost;

public static class AdviceRules
{
    public const string Version = "advice-v2";
    public static AdviceReply Rank(AdviceRequest request)
    {
        var p = request.Preferences ?? throw Invalid("Preferences required.");
        if (request.Candidates.Count > 500) throw Invalid("Narrow the candidate selection.");
        foreach (var level in new[] { p.Drainage, p.Aeration, p.WaterRetention })
            if (level != "" && level is not ("Low" or "Medium" or "High")) throw Invalid("Invalid physical preference.");
        if (p.HasPhMin != p.HasPhMax || p.HasPhMin && (p.PhMin < 0 || p.PhMax < p.PhMin || p.PhMax > 1400)) throw Invalid("Invalid pH interval.");
        if (p.HasMaxEc && (p.MaxEc < 0 || p.EcMethod is not ("SME" or "Extract1to2" or "PourThrough"))) throw Invalid("A comparable EC method is required.");
        if (p.UseContext != "" && p.UseContext is not ("Pot" or "InGround" or "Both")) throw Invalid("Invalid planting context.");
        if (p.RequiredInformation.Any(x => x is not ("Structure" or "Nutrition" or "Organic" or "Biology" or "Cleanliness"))) throw Invalid("Invalid information requirement.");
        var max = (p.PlantGroup != "" ? 3 : 0) + (p.Drainage != "" ? 3 : 0) + (p.Aeration != "" ? 3 : 0)
            + (p.WaterRetention != "" ? 3 : 0) + (p.HasPhMin ? 2 : 0) + (p.HasMaxEc ? 2 : 0) + (p.UseContext != "" ? 1 : 0);
        if (max == 0) throw Invalid("Enter a plant group, physical preference, pH, EC or planting context.");
        var matches = new List<(AdviceCandidate Candidate, AdviceMatch Match)>();
        foreach (var c in request.Candidates)
        {
            if (!c.IsActive || !c.IsReviewed || c.StockQty <= 0 || c.PriceVnd <= 0) continue;
            if (p.RequiredInformation.Any(x => !c.AvailableInformation.Contains(x)) || p.RequireSample && (!c.HasSampleAvailable || !c.SampleAvailable)) continue;
            var result = new AdviceMatch { SkuId = c.SkuId, MaxScore = max };
            bool Missing(string label) { result.Missing.Add(label); return p.AllowIncomplete; }
            bool Exact(string expected, string actual, int points, string label)
            {
                if (expected == "") return true;
                if (actual == "") return Missing(label + ": chưa có thông tin");
                if (expected != actual) return false;
                result.Score += points; result.Reasons.Add(label + ": khớp yêu cầu"); return true;
            }
            if (p.PlantGroup != "")
            {
                if (c.PlantGroups.Count == 0) { if (!Missing("Nhóm cây: nguồn chưa ghi nhận")) continue; }
                else if (!c.PlantGroups.Contains(p.PlantGroup)) continue;
                else { result.Score += 3; result.Reasons.Add("Nhóm cây: có trong thông tin được nguồn ghi nhận"); }
            }
            if (!Exact(p.Drainage, c.Drainage, 3, "Thoát nước") || !Exact(p.Aeration, c.Aeration, 3, "Thoáng khí") || !Exact(p.WaterRetention, c.WaterRetention, 3, "Giữ ẩm")) continue;
            if (p.HasPhMin)
            {
                if (!c.HasPhMin || !c.HasPhMax) { if (!Missing("pH: chưa có khoảng đo")) continue; }
                else if (c.PhMin < p.PhMin || c.PhMax > p.PhMax) continue;
                else { result.Score += 2; result.Reasons.Add("pH: toàn bộ khoảng công bố nằm trong yêu cầu"); }
            }
            if (p.HasMaxEc)
            {
                if (!c.HasEcMin || !c.HasEcMax || c.EcMethod != p.EcMethod) { if (!Missing("EC: thiếu số đo hoặc khác phương pháp, không so sánh")) continue; }
                else if (c.EcMax > p.MaxEc) continue;
                else { result.Score += 2; result.Reasons.Add("EC: không vượt mức tối đa với cùng phương pháp đo"); }
            }
            if (p.UseContext != "")
            {
                if (c.UseContext == "") { if (!Missing("Vị trí trồng: chưa có thông tin")) continue; }
                else if (c.UseContext != p.UseContext && c.UseContext != "Both") continue;
                else { result.Score++; result.Reasons.Add("Vị trí trồng: khớp thông tin nguồn"); }
            }
            if (result.Score > 0) matches.Add((c, result));
        }
        var reply = new AdviceReply { RuleVersion = Version };
        reply.Matches.AddRange(matches.OrderByDescending(x => x.Match.Score).ThenBy(x => x.Candidate.PriceVnd).ThenBy(x => x.Candidate.SkuId).Take(3).Select(x => x.Match));
        return reply;
    }
    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
