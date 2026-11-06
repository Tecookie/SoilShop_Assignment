namespace SoilShop.Domain;
public sealed class DailySummary
{
    public int Id { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string Status { get; set; } = "Queued";
    public decimal RevenueVnd { get; set; } = 0;
    public long SoldBags { get; set; } = 0;
    public int SaleCount { get; set; } = 0;
    public DateTime? SnapshotAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
