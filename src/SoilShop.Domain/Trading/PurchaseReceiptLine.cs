namespace SoilShop.Domain;
public sealed class PurchaseReceiptLine
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int SkuId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCostVnd { get; set; }
}
