using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> LeaveTypeExistsAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}