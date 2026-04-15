using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IPermissionRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}