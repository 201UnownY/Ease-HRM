using Ease_HRM.Application.DTOs.LeaveTypes;

namespace Ease_HRM.Application.Interfaces;

public interface ILeaveTypeService
{
    Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveTypeDto>> GetAllLeaveTypesAsync(CancellationToken cancellationToken = default);
}