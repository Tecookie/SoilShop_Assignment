namespace SoilShop.Domain;
public sealed class SaleLine
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int SkuId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPriceVnd { get; set; }
    public string? SkuLabelSnapshot { get; set; }
}
