using AI_rainfall_early_warning.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_rainfall_early_warning.Web.Controllers;

public class DataController : Controller
{
    private readonly ICsvImportService _csv;
    private readonly IAuditService _audit;

    public DataController(ICsvImportService csv, IAuditService audit)
    {
        _csv = csv;
        _audit = audit;
    }

    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(int regionId, IFormFile? file)
    {
        if (!FicheiroValido(file, out var erro))
        {
            TempData["Erro"] = erro;
            return RedirectToAction("Index");
        }

        try
        {
            // 1) Ler o conteudo do CSV como texto (3o parametro: csvstream)
            string csvContent;
            await using (var leitura = file!.OpenReadStream())
            using (var reader = new StreamReader(leitura))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            // 2) Abrir uma stream nova para o servico (4o parametro)
            await using var stream = file.OpenReadStream();

            var importados = await _csv.ImportAsync(regionId, file.FileName, csvContent, stream);

            await _audit.LogAsync("UploadCSV", $"{importados} registos de {file.FileName} (regiao {regionId})");
            TempData["Sucesso"] = $"{importados} registos importados com sucesso.";
        }
        catch (Exception ex)
        {
            await _audit.LogAsync("UploadCSVFalhou", $"{file!.FileName}: {ex.Message}");
            TempData["Erro"] = "Falha na importacao: " + ex.Message;
        }

        return RedirectToAction("Index");
    }

    private static bool FicheiroValido(IFormFile? file, out string erro)
    {
        erro = "";
        if (file == null || file.Length == 0)
        { erro = "Nenhum ficheiro seleccionado."; return false; }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
        { erro = "Apenas ficheiros .csv sao permitidos."; return false; }

        if (file.Length > 5 * 1024 * 1024)
        { erro = "Ficheiro demasiado grande (limite: 5 MB)."; return false; }

        return true;
    }
}