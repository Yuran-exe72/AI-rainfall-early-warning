using AI_rainfall_early_warning.ML.Models;
using Microsoft.ML;

namespace AI_rainfall_early_warning.ML.Services;

public interface IRainfallForecastService
{
    List<ForecastDay> Forecast(List<DailyClimateRecord> regionHistory, string modelPath, int days = 7);
}

public class RainfallForecastService : IRainfallForecastService
{
    public List<ForecastDay> Forecast(List<DailyClimateRecord> regionHistory, string modelPath, int days = 7)
    {
        var ml = new MLContext();
        var model = ml.Model.Load(modelPath, out _);
        var engine = ml.Model.CreatePredictionEngine<RainfallFeature, RainfallPrediction>(model);

        var series = regionHistory.OrderBy(r => r.Date).ToList();

        // Climatologia: média de chuva diária por mês (base histórica completa)
        var monthlyAvg = series
            .GroupBy(r => r.Date.Month)
            .ToDictionary(g => g.Key, g => g.Average(r => r.RainfallMm ?? 0));

        // Janela móvel com os últimos 14 dias; vamos estendendo com as previsões
        var window = series.Skip(Math.Max(0, series.Count - 14)).ToList();
        var lastDate = window.Last().Date;

        var results = new List<ForecastDay>();
        for (int d = 1; d <= days; d++)
        {
            var targetDate = lastDate.AddDays(d);
            var today = window.Last();

            var feature = new RainfallFeature
            {
                Month = targetDate.Month,
                RainLag1 = (float)(today.RainfallMm ?? 0),
                RainLag3 = (float)window.Skip(Math.Max(0, window.Count - 3)).Average(r => r.RainfallMm ?? 0),
                RainLag7 = (float)window.Skip(Math.Max(0, window.Count - 7)).Average(r => r.RainfallMm ?? 0),
                Rain7DaySum = (float)window.Skip(Math.Max(0, window.Count - 7)).Sum(r => r.RainfallMm ?? 0),
                SoilMoisture = (float)(today.SoilMoisture ?? 0.25),
                TemperatureC = (float)(today.TemperatureC ?? 25),
                Label = 0
            };

            var pred = engine.Predict(feature);
            double predicted = Math.Max(0, pred.Score);

            monthlyAvg.TryGetValue(targetDate.Month, out var avg);
            var risk = DroughtRiskClassifier.Classify(predicted, avg, today.SoilMoisture);

            results.Add(new ForecastDay
            {
                Date = targetDate,
                PredictedRainfallMm = Math.Round(predicted, 1),
                RiskLevel = risk
            });

            // Alimenta a janela com o dia previsto (previsão iterativa)
            window.Add(new DailyClimateRecord
            {
                RegionId = today.RegionId,
                Date = targetDate,
                RainfallMm = predicted,
                SoilMoisture = today.SoilMoisture,
                TemperatureC = today.TemperatureC
            });
            if (window.Count > 14) window.RemoveAt(0);
        }
        return results;
    }
}

/// <summary>Classifica o risco comparando a previsão com a média histórica do mês.</summary>
public static class DroughtRiskClassifier
{
    public static string Classify(double predictedRainMm, double monthlyAverageMm, double? soilMoisture)
    {
        var avg = Math.Max(monthlyAverageMm, 0.05);
        var ratio = predictedRainMm / avg;
        var sm = soilMoisture ?? 0.25;

        if (ratio < 0.30 && sm < 0.15) return "Emergência";
        if (ratio < 0.50 && sm < 0.25) return "Alerta";
        if (ratio < 0.70) return "Atenção";
        return "Normal";
    }
}