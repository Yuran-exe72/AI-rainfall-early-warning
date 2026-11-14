using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AI_rainfall_early_warning.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace AI_rainfall_early_warning.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
