using Ease_HRM.Application.DTOs.Attendance;

namespace Ease_HRM.Application.Interfaces;

public interface IAttendanceService
{
    Task CheckInAsync(CheckInRequest request, CancellationToken cancellationToken = default);
    Task CheckOutAsync(CheckOutRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordDto> GetDailySummaryAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(CancellationToken cancellationToken = default);
}