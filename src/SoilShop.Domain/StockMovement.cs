namespace SoilShop.Domain;
public sealed class StockMovement
{
    public long Id { get; set; }
    public int SkuId { get; set; }
    public string Kind { get; set; } = "";
    public int DeltaQty { get; set; }
    public int BalanceAfter { get; set; }
    public long StockVersionAfter { get; set; }
    public int? ReceiptId { get; set; }
    public int? SaleId { get; set; }
    public int? AdjustmentId { get; set; }
    public int ActorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
