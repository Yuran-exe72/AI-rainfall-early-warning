using AI_rainfall_early_warning.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace AI_rainfall_early_warning.ML.Services;

public class TrainingResult
{
    public int TrainSamples { get; set; }
    public int TestSamples { get; set; }
    public double R2 { get; set; }
    public double Rmse { get; set; }
    public string ModelPath { get; set; } = "";
}

public interface IRainfallTrainingService
{
    TrainingResult Train(IEnumerable<DailyClimateRecord> history, string modelPath);
}

public class RainfallTrainingService : IRainfallTrainingService
{
    public TrainingResult Train(IEnumerable<DailyClimateRecord> history, string modelPath)
    {
        var allRows = FeatureBuilder.BuildFeatureRows(history);

        // Divisão TEMPORAL (sem fuga de dados): primeiros 80% de cada região = treino
        var train = new List<RainfallFeature>();
        var test = new List<RainfallFeature>();
        foreach (var g in allRows.GroupBy(r => r.RegionId))
        {
            var list = g.ToList();
            int split = (int)(list.Count * 0.8);
            train.AddRange(list.Take(split));
            test.AddRange(list.Skip(split));
        }

        var ml = new MLContext(seed: 42);
        var trainData = ml.Data.LoadFromEnumerable(train);
        var testData = ml.Data.LoadFromEnumerable(test);

        var pipeline = ml.Transforms.Concatenate("Features",
                nameof(RainfallFeature.Month),
                nameof(RainfallFeature.RainLag1),
                nameof(RainfallFeature.RainLag3),
                nameof(RainfallFeature.RainLag7),
                nameof(RainfallFeature.Rain7DaySum),
                nameof(RainfallFeature.SoilMoisture),
                nameof(RainfallFeature.TemperatureC))
            .Append(ml.Regression.Trainers.FastForest(
                numberOfTrees: 200, minimumExampleCountPerLeaf: 5));

        var model = pipeline.Fit(trainData);
        var predictions = model.Transform(testData);
        var metrics = ml.Regression.Evaluate(predictions);

        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        ml.Model.Save(model, trainData.Schema, modelPath);

        return new TrainingResult
        {
            TrainSamples = train.Count,
            TestSamples = test.Count,
            R2 = metrics.RSquared,
            Rmse = metrics.RootMeanSquaredError,
            ModelPath = modelPath
        };
    }
}

/// <summary>Constrói as features com histórico (lags) a partir da série diária.</summary>
public static class FeatureBuilder
{
    public static List<RainfallFeature> BuildFeatureRows(IEnumerable<DailyClimateRecord> history)
    {
        var rows = new List<RainfallFeature>();
        foreach (var group in history.GroupBy(r => r.RegionId))
        {
            var series = group.OrderBy(r => r.Date).ToList();
            for (int i = 7; i < series.Count - 1; i++)
            {
                var today = series[i];
                var tomorrow = series[i + 1];
                rows.Add(new RainfallFeature
                {
                    RegionId = today.RegionId,
                    Month = tomorrow.Date.Month,
                    RainLag1 = (float)(today.RainfallMm ?? 0),
                    RainLag3 = (float)series.Skip(i - 2).Take(3).Average(r => r.RainfallMm ?? 0),
                    RainLag7 = (float)series.Skip(i - 6).Take(7).Average(r => r.RainfallMm ?? 0),
                    Rain7DaySum = (float)series.Skip(i - 6).Take(7).Sum(r => r.RainfallMm ?? 0),
                    SoilMoisture = (float)(today.SoilMoisture ?? 0.25),
                    TemperatureC = (float)(today.TemperatureC ?? 25),
                    Label = (float)(tomorrow.RainfallMm ?? 0)
                });
            }
        }
        return rows;
    }
}