using AutoMapper;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed class SoilMetadataService(ICatalogueRepository products, ISoilMetadataRepository repository, IMapper mapper)
{
    private static string? Text(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    public static void Option(string? value, IReadOnlyList<string> allowed, string field)
    {
        if (value is not null && !allowed.Contains(value, StringComparer.Ordinal)) throw new BusinessException(400, $"Invalid {field}.");
    }
    public static void Range(decimal? min, decimal? max, decimal limit, int decimals, string field)
    {
        if (min is null && max is null) return;
        if (min is null || max is null || min < 0 || max < min || max > limit || decimal.Round(min.Value, decimals) != min || decimal.Round(max.Value, decimals) != max)
            throw new BusinessException(400, $"{field} requires both ordered bounds within 0–{limit} and at most {decimals} decimal places.");
    }
    public async Task<SoilMetadataDto> GetAsync(int id, CancellationToken ct)
    {
        var product = await products.ProductAsync(id, ct) ?? throw new BusinessException(404, "Product not found.");
        var result = mapper.Map<SoilMetadataDto>(product); result.PlantGroups = await repository.PlantGroupsAsync(id, ct); return result;
    }
    public async Task<SoilMetadataDto> SaveAsync(int id, SoilMetadataInput input, CancellationToken ct)
    {
        Option(input.Drainage, SoilOptions.Levels, "drainage"); Option(input.Aeration, SoilOptions.Levels, "aeration");
        Option(input.WaterRetention, SoilOptions.Levels, "water retention"); Option(input.UseContext, SoilOptions.Contexts, "planting context");
        Option(input.EcMethod, SoilOptions.EcMethods, "EC method"); Range(input.PhMin, input.PhMax, 14, 2, "pH"); Range(input.EcMin, input.EcMax, 99999.999m, 3, "EC (mS/cm)");
        if ((input.EcMin is not null) != (input.EcMethod is not null)) throw new BusinessException(400, "EC bounds and measurement method must be supplied together.");
        var groups = input.PlantGroups ?? [];
        if (groups.Count > 8 || groups.Distinct(StringComparer.Ordinal).Count() != groups.Count || groups.Any(x => x is null || !SoilOptions.PlantGroups.ContainsKey(x))) throw new BusinessException(400, "Choose distinct supported plant groups.");
        var product = await products.ProductAsync(id, ct) ?? throw new BusinessException(404, "Product not found.");
        if (product.Version != input.ExpectedVersion) throw new BusinessException(409, "Product information changed. Reload before saving.");
        product.Drainage = input.Drainage; product.Aeration = input.Aeration; product.WaterRetention = input.WaterRetention;
        product.PhMin = input.PhMin; product.PhMax = input.PhMax; product.EcMin = input.EcMin; product.EcMax = input.EcMax; product.EcMethod = input.EcMethod;
        product.StructureNotes = Text(input.StructureNotes); product.NutritionNotes = Text(input.NutritionNotes);
        product.OrganicMatterNotes = Text(input.OrganicMatterNotes); product.BiologyNotes = Text(input.BiologyNotes); product.CleanlinessNotes = Text(input.CleanlinessNotes);
        product.UseContext = input.UseContext; product.SampleAvailable = input.SampleAvailable; product.Ingredients = Text(input.Ingredients);
        product.SourceReference = Text(input.SourceReference); product.ReviewedAt = null; product.ReviewedBy = null; product.Version++;
        await repository.SaveMetadataAsync(product, groups, ct); return await GetAsync(id, ct);
    }
    public async Task<SoilMetadataDto> ReviewAsync(int id, ReviewMetadataInput input, int actorId, CancellationToken ct)
    {
        var product = await products.ProductAsync(id, ct) ?? throw new BusinessException(404, "Product not found.");
        if (product.Version != input.ExpectedVersion) throw new BusinessException(409, "Information changed. Review the current version first.");
        if (string.IsNullOrWhiteSpace(product.SourceReference)) throw new BusinessException(400, "A label/document/supplier source is required before review.");
        product.ReviewedAt = DateTime.UtcNow; product.ReviewedBy = actorId; product.Version++;
        await repository.SaveMetadataAsync(product, null, ct); return await GetAsync(id, ct);
    }
}
