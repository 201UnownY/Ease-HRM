using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface ILeaveTypeRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
    Task<List<LeaveType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}