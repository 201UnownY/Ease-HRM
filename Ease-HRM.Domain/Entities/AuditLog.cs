namespace Ease_HRM.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Details { get; set; }
}
