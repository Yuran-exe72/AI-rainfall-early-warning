using AI_rainfall_early_warning.Domain.Entities;

namespace AI_rainfall_early_warning.Web.Models;

public class ForecastViewModel
{
    public List<Region> Regions { get; set; } = new();
    public bool ModelExists { get; set; }
    public double? R2 { get; set; }
    public double? Rmse { get; set; }
    public string? TrainMessage { get; set; }
    public int? SelectedRegionId { get; set; }
    public string? SelectedRegionName { get; set; }
    public List<object>? Forecast { get; set; }
    public string? ErrorMessage { get; set; }
}