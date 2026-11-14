using AI_rainfall_early_warning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalObservacoes = await _db.ClimateObservations.CountAsync();
        ViewBag.TotalRegioes = await _db.Regions.CountAsync();
        ViewBag.DiasMonitorizados = await _db.ClimateObservations
            .Select(o => o.Date).Distinct().CountAsync();

        var last = await _db.DataImports
            .OrderByDescending(i => i.ImportedAt)
            .FirstOrDefaultAsync();
        ViewBag.UltimaImportacao = last is null
            ? "—"
            : $"{last.ImportedAt:g} · {last.FileName}";

        return View();
    }
}