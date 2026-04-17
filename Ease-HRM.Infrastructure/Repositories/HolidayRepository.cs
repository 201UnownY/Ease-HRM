using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly AppDbContext _context;

    public HolidayRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Holiday>> GetByMonthAsync(Guid? orgUnitId, int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        return _context.Holidays
            .AsNoTracking()
            .Where(x => x.Date >= start && x.Date < end && (x.OrgUnitId == null || x.OrgUnitId == orgUnitId))
            .ToListAsync(cancellationToken);
    }
}
