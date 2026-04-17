using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IHolidayRepository
{
    Task<List<Holiday>> GetByMonthAsync(Guid? orgUnitId, int year, int month, CancellationToken cancellationToken = default);
}
