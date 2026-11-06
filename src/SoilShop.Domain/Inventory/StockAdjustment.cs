namespace SoilShop.Domain;
public sealed class StockAdjustment
{
    public int Id { get; set; }
    public Guid RequestKey { get; set; }
    public int SkuId { get; set; }
    public int? StockCountId { get; set; }
    public int DeltaQty { get; set; }
    public string Reason { get; set; } = "";
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
