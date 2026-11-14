using AI_rainfall_early_warning.Application.Services;
using AI_rainfall_early_warning.Domain.Constants;
using AI_rainfall_early_warning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_rainfall_early_warning.Web.Controllers;

[Authorize]
public class DataController : Controller
{
    private readonly ICsvImportService _importService;
    private readonly ApplicationDbContext _db;

    public DataController(ICsvImportService importService, ApplicationDbContext db)
    {
        _importService = importService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var imports = await _db.DataImports
            .OrderByDescending(i => i.ImportedAt)
            .Take(50)
            .ToListAsync();
        return View(imports);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int regionId, string source, IFormFile file)
    {
        if (file is null || file.Length == 0)
            ModelState.AddModelError(string.Empty, "Selecione um ficheiro CSV.");
        else if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(string.Empty, "Apenas ficheiros .csv são aceites.");

        if (ModelState.IsValid)
        {
            await using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(regionId, source, file.FileName, stream);

            if (result.Success)
            {
                TempData["Success"] = $"✅ {file.FileName}: {result.RowsImported} novas, {result.RowsUpdated} actualizadas, {result.RowsRejected} rejeitadas.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Erro desconhecido.");
        }

        await PopulateDropdownsAsync();
        return View();
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Regions = await _db.Regions.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Sources = new List<string>
        {
            DomainConstants.Sources.Chirps,
            DomainConstants.Sources.Era5Land,
            DomainConstants.Sources.Smap
        };
    }
}