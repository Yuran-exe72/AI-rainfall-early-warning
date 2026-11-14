using AI_rainfall_early_warning.Application.Services;
using AI_rainfall_early_warning.Infrastructure.Data;
using AI_rainfall_early_warning.ML.Models;
using AI_rainfall_early_warning.ML.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Web.Controllers;

/// <summary>Endpoint 1: lista de regioes monitorizadas.</summary>
[ApiController]
[Route("api/regions")]
[Authorize]
public class ApiRegionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ApiRegionsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRegions()
    {
        var regions = await _db.Regions.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();
        return Ok(regions);
    }
}

/// <summary>Endpoint 2: observacoes climaticas com filtros e paginacao.</summary>
[ApiController]
[Route("api/observations")]
[Authorize]
public class ApiObservationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ApiObservationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetObservations(
        [FromQuery] int? regionId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.ClimateObservations.AsNoTracking();
        if (regionId.HasValue) query = query.Where(o => o.RegionId == regionId);
        if (from.HasValue) query = query.Where(o => o.Date >= from);
        if (to.HasValue) query = query.Where(o => o.Date <= to);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.RegionId,
                o.Date,
                o.RainfallMm,
                o.SoilMoisture,
                o.TemperatureC
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}

/// <summary>Endpoint 3: previsao de 7 dias com nivel de risco por regiao.</summary>
[ApiController]
[Route("api/forecast")]
[Authorize]
public class ApiForecastController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IRainfallForecastService _forecaster;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public ApiForecastController(ApplicationDbContext db, IRainfallForecastService forecaster,
        IAuditService audit, IWebHostEnvironment env)
    {
        _db = db; _forecaster = forecaster; _audit = audit; _env = env;
    }

    private string ModelPath => Path.Combine(_env.ContentRootPath, "App_Data", "rainfall-model.zip");

    [HttpGet("{regionId}")]
    public async Task<IActionResult> GetForecast(int regionId)
    {
        if (!System.IO.File.Exists(ModelPath))
            return NotFound(new { error = "Modelo nao treinado. Treine primeiro na pagina Previsao." });

        var daily = await LoadDailyAsync();
        var history = daily.Where(r => r.RegionId == regionId).ToList();
        if (history.Count == 0)
            return NotFound(new { error = "Regiao sem dados de observacao." });

        var forecast = _forecaster.Forecast(history, ModelPath, 7);
        await _audit.LogAsync("PrevisaoAPI", $"Regiao={regionId}, 7 dias");
        return Ok(new { regionId, horizonDays = 7, forecast });
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