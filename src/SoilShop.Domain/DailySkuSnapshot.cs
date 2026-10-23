namespace SoilShop.Domain;
public sealed class DailySkuSnapshot
{
    public long Id { get; set; }
    public int SummaryId { get; set; }
    public int SkuId { get; set; }
    public int StockQty { get; set; }
}
