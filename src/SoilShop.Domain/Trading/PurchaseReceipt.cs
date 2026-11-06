namespace SoilShop.Domain;
public sealed class PurchaseReceipt
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int SupplierId { get; set; }
    public string Status { get; set; } = "Draft";
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmationKey { get; set; }
    public decimal? TotalVnd { get; set; }
    public int? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancellationKey { get; set; }
    public string? CancelReason { get; set; }
    public long Version { get; set; } = 1;
}
