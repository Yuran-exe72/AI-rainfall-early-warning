namespace AI_rainfall_early_warning.Application.Services;

public interface IAuditService
{
    Task LogAsync(string action, string details);
}