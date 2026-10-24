namespace SoilShop.Domain;
public sealed class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProductType { get; set; } = ProductTypes.Unclassified;
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public string? Ingredients { get; set; }
    public string? Drainage { get; set; }
    public string? WaterRetention { get; set; }
    public decimal? PhMin { get; set; }
    public decimal? PhMax { get; set; }
    public string? StorageNote { get; set; }
    public string? SourceReference { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
