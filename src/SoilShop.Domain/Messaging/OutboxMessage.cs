namespace SoilShop.Domain;
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; } = 0;
    public string? LastError { get; set; }
}
