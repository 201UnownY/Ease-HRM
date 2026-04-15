using Ease_HRM.Application.DTOs.RolePermissions;

namespace Ease_HRM.Application.Interfaces;

public interface IRolePermissionService
{
    Task<RolePermissionDto> AssignPermissionToRoleAsync(AssignPermissionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolePermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}