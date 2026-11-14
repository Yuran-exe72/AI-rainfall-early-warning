using Microsoft.AspNetCore.Identity;

namespace AI_rainfall_early_warning.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}