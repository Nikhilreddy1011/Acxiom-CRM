namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? recordId, string? oldValue, string? newValue, string? userId = null, string? userName = null, string? ipAddress = null);
}
