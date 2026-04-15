namespace Ease_HRM.Application.DTOs.RolePermissions;

public class AssignPermissionRequest
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}