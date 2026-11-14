namespace AI_rainfall_early_warning.Domain.Entities;

public class ClimateObservation
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public DateTime Date { get; set; }
    public string Source { get; set; } = string.Empty;

    public double? RainfallMm { get; set; }
    public double? SoilMoisture { get; set; }
    public double? TemperatureC { get; set; }

    public int ImportId { get; set; }
    public DataImport Import { get; set; } = null!;
}