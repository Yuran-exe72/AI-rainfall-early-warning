namespace AI_rainfall_early_warning.Domain.Entities;

public class ModelVersion
{
    public int Id { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public double MicroAccuracy { get; set; }
    public double MacroAccuracy { get; set; }
    public string MetricsJson { get; set; } = "{}";
    public string ModelFilePath { get; set; } = string.Empty;
    public DateTime TrainedAt { get; set; }
    public bool IsActive { get; set; }
    public int TrainingRows { get; set; }
    public int TestRows { get; set; }
}