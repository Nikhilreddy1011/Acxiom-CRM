using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? recordId,
        string? oldValue,
        string? newValue,
        string? userId = null,
        string? userName = null,
        string? ipAddress = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (string.IsNullOrEmpty(userId) && httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(userName) && httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userName = httpContext.User.Identity.Name;
        }

        if (string.IsNullOrEmpty(ipAddress) && httpContext != null)
        {
            ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
        }

        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
