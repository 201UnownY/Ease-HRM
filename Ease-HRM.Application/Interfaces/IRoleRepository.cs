using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IRoleRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}