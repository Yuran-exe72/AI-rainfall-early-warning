using AI_rainfall_early_warning.Infrastructure.Data;
using AI_rainfall_early_warning.Infrastructure.Identity;
using AI_rainfall_early_warning.ML.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClimaAlerta MZ API",
        Version = "v1",
        Description = "API REST de observacoes climaticas e previsao de precipitacao para as regioes de Mabote e Govuro, Inhambane."
    });
});

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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClimaAlerta MZ API v1");
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();