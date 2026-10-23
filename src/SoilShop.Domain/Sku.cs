namespace SoilShop.Domain;
public sealed class Sku
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Code { get; set; } = "";
    public decimal PackageAmount { get; set; }
    public string PackageUnit { get; set; } = "";
    public decimal SalePriceVnd { get; set; }
    public decimal? ReferenceCostVnd { get; set; }
    public int StockQty { get; set; } = 0;
    public long StockVersion { get; set; } = 0;
    public int LowStockThreshold { get; set; } = 5;
    public int TargetStock { get; set; } = 30;
    public int? PreferredSupplierId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
