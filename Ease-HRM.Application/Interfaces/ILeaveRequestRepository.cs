using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Employee?> GetEmployeeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Employee>> GetHierarchyEmployeesAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> LeaveTypeExistsAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
    Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}