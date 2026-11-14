using AI_rainfall_early_warning.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AI_rainfall_early_warning.Infrastructure.Identity;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Criar os dois perfis
        foreach (var role in new[] { "Administrador", "Investigador" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Administrador (usa a MESMA password com que ja entras!)
        const string adminEmail = "admin@drought.mz";
        const string adminPassword = "Admin123"; // <- substitui pela tua

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            if ((await userManager.CreateAsync(admin, adminPassword)).Succeeded)
                await userManager.AddToRoleAsync(admin, "Administrador");
        }
        else if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            await userManager.AddToRoleAsync(admin, "Administrador");
        }

        // 3. Investigador (utilizador "normal" para demonstrar as roles)
        const string invEmail = "Alfredo@drought.mz";
        var inv = await userManager.FindByEmailAsync(invEmail);
        if (inv == null)
        {
            inv = new ApplicationUser
            {
                UserName = invEmail,
                Email = invEmail,
                EmailConfirmed = true
            };
            if ((await userManager.CreateAsync(inv, "Alfredo123")).Succeeded)
                await userManager.AddToRoleAsync(inv, "Alfredo");
        }
    }
}