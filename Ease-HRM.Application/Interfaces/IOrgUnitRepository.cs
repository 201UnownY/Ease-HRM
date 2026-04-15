using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IOrgUnitRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid orgUnitId, CancellationToken cancellationToken = default);
    Task AddAsync(OrgUnit orgUnit, CancellationToken cancellationToken = default);
    Task<List<OrgUnit>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}