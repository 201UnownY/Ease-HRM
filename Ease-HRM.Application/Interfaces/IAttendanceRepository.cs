using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IAttendanceRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<AttendanceSession?> GetActiveSessionAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<AttendanceSession>> GetSessionsByDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<AttendanceSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default);
    Task<List<(Guid EmployeeId, DateTime Start, DateTime End)>> GetApprovedLeaveRangesAsync(CancellationToken cancellationToken = default);
    Task<AttendancePolicy?> GetEffectivePolicyAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<AttendancePolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingPolicyAsync(DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludePolicyId = null, CancellationToken cancellationToken = default);
    Task AddPolicyAsync(AttendancePolicy policy, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task<bool> HasApprovedLeaveAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task AddSessionAsync(AttendanceSession session, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}