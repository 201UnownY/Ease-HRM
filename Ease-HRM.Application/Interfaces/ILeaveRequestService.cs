using Ease_HRM.Application.DTOs.LeaveRequests;

namespace Ease_HRM.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> ApplyLeaveAsync(ApplyLeaveRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> ApproveLeaveAsync(ApproveLeaveRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> RejectLeaveAsync(RejectLeaveRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetAllLeaveRequestsAsync(CancellationToken cancellationToken = default);
}