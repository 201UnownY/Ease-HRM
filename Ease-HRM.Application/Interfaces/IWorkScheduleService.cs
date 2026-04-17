using Ease_HRM.Application.DTOs.WorkSchedules;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IWorkScheduleService
{
    Task<WorkSchedule> CreateAsync(CreateWorkScheduleRequest request, CancellationToken cancellationToken = default);
    Task<WorkSchedule> UpdateAsync(Guid id, UpdateWorkScheduleRequest request, CancellationToken cancellationToken = default);
}
