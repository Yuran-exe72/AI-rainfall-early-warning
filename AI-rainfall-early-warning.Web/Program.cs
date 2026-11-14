using AI_rainfall_early_warning.Infrastructure.Data;
using AI_rainfall_early_warning.Infrastructure.Identity;
using AI_rainfall_early_warning.ML.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AI_rainfall_early_warning.Application.Services.ICsvImportService,
    AI_rainfall_early_warning.Infrastructure.Services.CsvImportService>();

builder.Services.AddScoped<AI_rainfall_early_warning.ML.Services.IRainfallTrainingService,
    AI_rainfall_early_warning.ML.Services.RainfallTrainingService>();

builder.Services.AddScoped<AI_rainfall_early_warning.ML.Services.IRainfallForecastService,
    AI_rainfall_early_warning.ML.Services.RainfallForecastService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbInitializer.SeedAsync(services);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();