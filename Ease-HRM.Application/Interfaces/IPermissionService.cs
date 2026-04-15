using Ease_HRM.Application.DTOs.Permissions;

namespace Ease_HRM.Application.Interfaces;

public interface IPermissionService
{
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}