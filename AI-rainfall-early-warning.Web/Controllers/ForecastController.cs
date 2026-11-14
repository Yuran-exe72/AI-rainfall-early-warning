using AI_rainfall_early_warning.Application.Services;
using AI_rainfall_early_warning.Domain.Entities;
using AI_rainfall_early_warning.Infrastructure.Data;
using AI_rainfall_early_warning.ML.Models;
using AI_rainfall_early_warning.ML.Services;
using AI_rainfall_early_warning.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Web.Controllers;

public class ForecastController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IRainfallTrainingService _trainer;
    private readonly IRainfallForecastService _forecaster;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public ForecastController(
        ApplicationDbContext db,
        IRainfallTrainingService trainer,
        IRainfallForecastService forecaster,
        IAuditService audit,
        IWebHostEnvironment env)
    {
        _db = db;
        _trainer = trainer;
        _forecaster = forecaster;
        _audit = audit;
        _env = env;
    }

    private string ModelPath =>
        Path.Combine(_env.ContentRootPath, "App_Data", "rainfall-model.zip");

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Train()
    {
        var vm = await BuildViewModelAsync();

        try
        {
            var daily = await LoadDailyAsync();
            if (daily.Count == 0)
            {
                vm.ErrorMessage = "Nao ha observacoes na base de dados. Importe os CSV primeiro.";
                return View("Index", vm);
            }

            // <<< UNICA LINHA A CONFIRMAR: o nome do metodo de treino.
            // Escreve "_trainer." e o IntelliSense mostra o nome real.
            // Se for diferente de "Train", troca SO o nome aqui embaixo.
            var result = _trainer.Train(daily, ModelPath);

            vm.R2 = result.R2;
            vm.Rmse = result.Rmse;
            vm.TrainMessage = "Modelo treinado com sucesso.";

            await _audit.LogAsync("TreinoModelo", $"R2={result.R2:0.000} RMSE={result.Rmse:0.00} mm");
        }
        catch (Exception ex)
        {
            await _audit.LogAsync("TreinoModeloFalhou", ex.Message);
            vm.ErrorMessage = "Falha no treino: " + ex.Message;
        }

        return View("Index", vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Predict(int regionId)
    {
        var vm = await BuildViewModelAsync();
        vm.SelectedRegionId = regionId;

        if (!System.IO.File.Exists(ModelPath))
        {
            vm.ErrorMessage = "Modelo nao treinado. Um administrador deve treinar primeiro.";
            return View("Index", vm);
        }

        var region = await _db.Regions.FirstOrDefaultAsync(r => r.Id == regionId);
        if (region == null)
        {
            vm.ErrorMessage = "Regiao desconhecida.";
            return View("Index", vm);
        }
        vm.SelectedRegionName = region.Name;

        try
        {
            var daily = await LoadDailyAsync();
            var history = daily.Where(r => r.RegionId == regionId).ToList();
            if (history.Count == 0)
            {
                vm.ErrorMessage = $"A regiao {region.Name} nao tem observacoes.";
                return View("Index", vm);
            }

            vm.Forecast = _forecaster.Forecast(history, ModelPath, 7)
                .Cast<object>()
                .ToList();

            await _audit.LogAsync("Previsao", $"Regiao={region.Name}, 7 dias");
        }
        catch (Exception ex)
        {
            await _audit.LogAsync("PrevisaoFalhou", $"{region.Name}: {ex.Message}");
            vm.ErrorMessage = "Falha na previsao: " + ex.Message;
        }

        return View("Index", vm);
    }

    private async Task<ForecastViewModel> BuildViewModelAsync()
    {
        return new ForecastViewModel
        {
            Regions = await _db.Regions.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
            ModelExists = System.IO.File.Exists(ModelPath)
        };
    }

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