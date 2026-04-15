namespace Ease_HRM.Application.DTOs.UserRoles;

public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}