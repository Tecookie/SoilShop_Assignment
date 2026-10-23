namespace SoilShop.Domain;
public sealed class BackupRun
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public DateOnly? ScheduledDate { get; set; }
    public int? RequestedBy { get; set; }
    public string Status { get; set; } = "Queued";
    public string? Filename { get; set; }
    public bool Retained { get; set; } = false;
    public int Attempts { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}
