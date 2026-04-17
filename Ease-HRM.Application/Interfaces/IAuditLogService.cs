namespace Ease_HRM.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityName, Guid? entityId = null, string? details = null, CancellationToken cancellationToken = default);
}
