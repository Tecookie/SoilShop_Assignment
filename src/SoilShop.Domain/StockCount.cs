namespace SoilShop.Domain;
public sealed class StockCount
{
    public int Id { get; set; }
    public int SkuId { get; set; }
    public int ExpectedQty { get; set; }
    public long ExpectedStockVersion { get; set; }
    public int ActualQty { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int CountedBy { get; set; }
    public DateTime CountedAt { get; set; } = DateTime.UtcNow;
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public long Version { get; set; } = 1;
}
