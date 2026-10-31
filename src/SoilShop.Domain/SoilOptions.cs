namespace SoilShop.Domain;

public static class SoilOptions
{
    public static IReadOnlyList<string> Levels { get; } = Array.AsReadOnly<string>(["Low", "Medium", "High"]);
    public static IReadOnlyList<string> EcMethods { get; } = Array.AsReadOnly<string>(["SME", "Extract1to2", "PourThrough", "Other"]);
    public static IReadOnlyList<string> Contexts { get; } = Array.AsReadOnly<string>(["Pot", "InGround", "Both"]);
    public static IReadOnlyDictionary<string, string> PlantGroups { get; } = new Dictionary<string, string>
    {
        ["General"] = "Cây trồng thông dụng", ["Foliage"] = "Cây kiểng lá", ["Flowering"] = "Cây hoa",
        ["Succulent"] = "Sen đá / xương rồng", ["Orchid"] = "Lan", ["Seedling"] = "Cây ươm",
        ["Bonsai"] = "Bonsai", ["AcidLoving"] = "Cây ưa axit"
    }.AsReadOnly();
}
