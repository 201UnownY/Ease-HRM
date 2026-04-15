using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IAttendanceRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<AttendanceSession?> GetActiveSessionAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<AttendanceSession>> GetSessionsByDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<AttendanceRecord?> GetRecordByDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<AttendancePolicy?> GetActivePolicyAsync(CancellationToken cancellationToken = default);
    Task<bool> HasApprovedLeaveAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task AddSessionAsync(AttendanceSession session, CancellationToken cancellationToken = default);
    Task AddRecordAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
    Task<List<AttendanceRecord>> GetAllRecordsAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}