using AI_rainfall_early_warning.Infrastructure.Data;
using AI_rainfall_early_warning.ML.Models;
using AI_rainfall_early_warning.ML.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Web.Controllers;

[Authorize]
public class PredictionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IRainfallTrainingService _trainer;
    private readonly IRainfallForecastService _forecaster;
    private readonly IWebHostEnvironment _env;

    public PredictionController(ApplicationDbContext db, IRainfallTrainingService trainer,
        IRainfallForecastService forecaster, IWebHostEnvironment env)
    {
        _db = db; _trainer = trainer; _forecaster = forecaster; _env = env;
    }

    private string ModelPath => Path.Combine(_env.ContentRootPath, "App_Data", "rainfall-model.zip");

    public IActionResult Index()
    {
        ViewBag.ModelExists = System.IO.File.Exists(ModelPath);
        return View();
    }

    [HttpGet]
    public IActionResult Train() => View((TrainingResult?)null);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Train")]
    public async Task<IActionResult> TrainPost()
    {
        var daily = await LoadDailyAsync();
        if (daily.Count < 30)
        {
            TempData["Error"] = "Dados insuficientes para treinar o modelo.";
            return RedirectToAction(nameof(Index));
        }

        var result = _trainer.Train(daily, ModelPath);
        return View("Train", result);
    }

    [HttpGet]
    public async Task<IActionResult> Forecast(int? regionId)
    {
        if (!System.IO.File.Exists(ModelPath))
        {
            TempData["Error"] = "Treina primeiro o modelo (menu Previsão → Treinar).";
            return RedirectToAction(nameof(Train));
        }

        var regions = await _db.Regions.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        var selected = regionId ?? regions.First().Id;

        ViewBag.Regions = regions;
        ViewBag.SelectedId = selected;
        ViewBag.SelectedName = regions.First(r => r.Id == selected).Name;

        var daily = await LoadDailyAsync();
        var regionHistory = daily.Where(r => r.RegionId == selected).ToList();

        ViewBag.LastDataDate = regionHistory.Last().Date;
        ViewBag.Rain7Days = Math.Round(regionHistory.Skip(Math.Max(0, regionHistory.Count - 7))
            .Sum(r => r.RainfallMm ?? 0), 1);
        ViewBag.SoilMoisture = regionHistory.Last().SoilMoisture;
        ViewBag.Temperature = regionHistory.Last().TemperatureC;

        return View(_forecaster.Forecast(regionHistory, ModelPath, 7));
    }

    /// <summary>Junta as 3 fontes (CHIRPS/ERA5/SMAP) num registo diário por região.</summary>
    private async Task<List<DailyClimateRecord>> LoadDailyAsync()
    {
        var obs = await _db.ClimateObservations.AsNoTracking().ToListAsync();
        return obs
            .GroupBy(o => new { o.RegionId, o.Date })
            .Select(g => new DailyClimateRecord
            {
                RegionId = g.Key.RegionId,
                Date = g.Key.Date,
                RainfallMm = g.Max(o => o.RainfallMm),
                SoilMoisture = g.Max(o => o.SoilMoisture),
                TemperatureC = g.Max(o => o.TemperatureC)
            })
            .OrderBy(r => r.RegionId).ThenBy(r => r.Date)
            .ToList();
    }
}