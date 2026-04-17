using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IUserRoleRepository
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> MappingExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserRole>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}