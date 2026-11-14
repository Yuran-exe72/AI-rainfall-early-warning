namespace AI_rainfall_early_warning.ML.Models;

/// <summary>Registo diário "neutro" — a Web junta as 3 fontes e passa isto ao ML.</summary>
public class DailyClimateRecord
{
    public int RegionId { get; set; }
    public DateTime Date { get; set; }
    public double? RainfallMm { get; set; }
    public double? SoilMoisture { get; set; }
    public double? TemperatureC { get; set; }
}

/// <summary>Linha de features para treinar/prever (ML.NET).</summary>
public class RainfallFeature
{
    public int RegionId { get; set; }
    public float Month { get; set; }
    public float RainLag1 { get; set; }
    public float RainLag3 { get; set; }
    public float RainLag7 { get; set; }
    public float Rain7DaySum { get; set; }
    public float SoilMoisture { get; set; }
    public float TemperatureC { get; set; }
    public float Label { get; set; } // chuva do dia seguinte (mm)
}

public class RainfallPrediction
{
    public float Score { get; set; }
}

public class ForecastDay
{
    public DateTime Date { get; set; }
    public double PredictedRainfallMm { get; set; }
    public string RiskLevel { get; set; } = "";
}