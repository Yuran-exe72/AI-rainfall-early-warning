namespace AI_rainfall_early_warning.Domain.Entities;

public class Prediction
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public DateTime TargetPeriod { get; set; }
    public string PredictedRisk { get; set; } = string.Empty;
    public double Confidence { get; set; }

    public int ModelVersionId { get; set; }
    public ModelVersion ModelVersion { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public string? ObservedRisk { get; set; }
    public bool AlertGenerated { get; set; }
}