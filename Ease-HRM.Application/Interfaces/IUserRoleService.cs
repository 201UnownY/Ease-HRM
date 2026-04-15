using Ease_HRM.Application.DTOs.UserRoles;

namespace Ease_HRM.Application.Interfaces;

public interface IUserRoleService
{
    Task<UserRoleDto> AssignRoleToUserAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
}