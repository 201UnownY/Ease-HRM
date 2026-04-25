using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IWorkScheduleRepository
{
    Task<bool> IsWorkingDay(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<int> GetWorkingDays(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<HashSet<DateTime>> GetWorkingDateSet(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<Dictionary<DateTime, decimal>> GetWorkingDateWeights(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<WorkSchedule?> GetEffectiveScheduleAsync(Guid employeeId, Guid? orgUnitId, DateTime date, CancellationToken cancellationToken = default);
    Task<WorkSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingScheduleAsync(Guid? employeeId, Guid? orgUnitId, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeScheduleId = null, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
