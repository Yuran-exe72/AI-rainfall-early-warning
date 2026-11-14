namespace AI_rainfall_early_warning.Domain.Entities;

public class Alert
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Novo";

    public DateTime CreatedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}