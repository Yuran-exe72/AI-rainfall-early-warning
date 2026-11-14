namespace AI_rainfall_early_warning.Domain.Entities;

public class Region
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public ICollection<ClimateObservation> Observations { get; set; } = new List<ClimateObservation>();
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}