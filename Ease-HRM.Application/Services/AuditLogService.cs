using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogService(IAuditLogRepository auditLogRepository, ICurrentUserService currentUserService)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(string action, string entityName, Guid? entityId = null, string? details = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entityName))
        {
            return;
        }

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action.Trim(),
            EntityName = entityName.Trim(),
            EntityId = entityId,
            Details = details,
            PerformedBy = _currentUserService.UserId ?? Guid.Empty,
            PerformedAt = DateTime.UtcNow
        };

        try
        {
            await _auditLogRepository.AddAsync(log, cancellationToken);
            await _auditLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Audit must never break business flow.
        }
    }
}
