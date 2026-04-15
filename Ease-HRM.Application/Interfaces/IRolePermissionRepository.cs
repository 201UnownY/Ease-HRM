using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<bool> MappingExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> PermissionExistsAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> RoleHasPermissionAsync(string roleName, string permissionName, CancellationToken cancellationToken = default);
    Task<List<RolePermission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}