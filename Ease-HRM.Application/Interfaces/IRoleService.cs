using Ease_HRM.Application.DTOs.Roles;

namespace Ease_HRM.Application.Interfaces;

public interface IRoleService
{
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
}