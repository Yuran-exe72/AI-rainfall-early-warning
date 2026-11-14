using AI_rainfall_early_warning.Application.Services;
using AI_rainfall_early_warning.Domain.Entities;
using AI_rainfall_early_warning.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace AI_rainfall_early_warning.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string details)
    {
        var ctx = _http.HttpContext;
        _db.AuditLogs.Add(new AuditLog
        {
            UserName = ctx?.User?.Identity?.Name ?? "anonimo",
            Action = action,
            Details = details,
            IpAddress = ctx?.Connection?.RemoteIpAddress?.ToString() ?? "-",
            TimestampUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}