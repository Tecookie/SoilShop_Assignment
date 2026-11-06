using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record SoilMetadataInput(long ExpectedVersion,
    string? Drainage = null, string? Aeration = null, string? WaterRetention = null,
    decimal? PhMin = null, decimal? PhMax = null, decimal? EcMin = null, decimal? EcMax = null,
    string? EcMethod = null, string? UseContext = null, bool? SampleAvailable = null,
    [MaxLength(3000)] string? StructureNotes = null, [MaxLength(3000)] string? NutritionNotes = null,
    [MaxLength(3000)] string? OrganicMatterNotes = null, [MaxLength(3000)] string? BiologyNotes = null,
    [MaxLength(3000)] string? CleanlinessNotes = null, [MaxLength(5000)] string? Ingredients = null,
    [MaxLength(2000)] string? SourceReference = null, List<string>? PlantGroups = null);
public sealed record ReviewMetadataInput(long ExpectedVersion);
public sealed class SoilMetadataDto
{
    public int ProductId { get; set; }
    public long Version { get; set; }
    public string? Drainage { get; set; }
    public string? Aeration { get; set; }
    public string? WaterRetention { get; set; }
    public decimal? PhMin { get; set; }
    public decimal? PhMax { get; set; }
    public decimal? EcMin { get; set; }
    public decimal? EcMax { get; set; }
    public string? EcMethod { get; set; }
    public string? UseContext { get; set; }
    public bool? SampleAvailable { get; set; }
    public string? StructureNotes { get; set; }
    public string? NutritionNotes { get; set; }
    public string? OrganicMatterNotes { get; set; }
    public string? BiologyNotes { get; set; }
    public string? CleanlinessNotes { get; set; }
    public string? Ingredients { get; set; }
    public string? SourceReference { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public IReadOnlyList<string> PlantGroups { get; set; } = [];
}
