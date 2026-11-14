using AI_rainfall_early_warning.Domain.Entities;
using AI_rainfall_early_warning.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AI_rainfall_early_warning.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        string[] roles = { "Admin", "Investigador" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@drought.mz";
        const string adminPassword = "Admin@123";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrador do Sistema"
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await db.Regions.AnyAsync())
        {
            db.Regions.AddRange(
                new Region { Name = "Mabote", Latitude = -23.86, Longitude = 33.74 },
                new Region { Name = "Govuro", Latitude = -21.60, Longitude = 34.90 }
            );
            await db.SaveChangesAsync();
        }
    }
}