namespace SoilShop.Domain;

public static class ProductTypes
{
    public const string Unclassified = "Unclassified";
    public static IReadOnlyList<string> Values { get; } = Array.AsReadOnly<string>(
        [Unclassified, "GeneralPurpose", "FoliageMix", "FloweringMix", "SucculentMix", "OrchidMix", "SeedStartingMix"]);
    public static bool IsValid(string value) => Values.Contains(value, StringComparer.Ordinal);
}
