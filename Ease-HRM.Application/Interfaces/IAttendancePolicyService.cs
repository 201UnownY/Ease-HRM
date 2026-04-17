using Ease_HRM.Application.DTOs.Attendance;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IAttendancePolicyService
{
    Task<AttendancePolicy> CreateAsync(CreateAttendancePolicyRequest request, CancellationToken cancellationToken = default);
    Task<AttendancePolicy> UpdateAsync(UpdateAttendancePolicyRequest request, CancellationToken cancellationToken = default);
}
