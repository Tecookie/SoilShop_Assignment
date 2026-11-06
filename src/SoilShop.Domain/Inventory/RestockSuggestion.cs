namespace SoilShop.Domain;
public sealed class RestockSuggestion
{
    public int Id { get; set; }
    public int SkuId { get; set; }
    public int? SupplierId { get; set; }
    public int ObservedQty { get; set; }
    public int SuggestedQty { get; set; }
    public string Status { get; set; } = "Open";
    public int? ReceiptId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
