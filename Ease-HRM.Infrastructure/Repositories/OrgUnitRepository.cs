using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class OrgUnitRepository : IOrgUnitRepository
{
    private readonly AppDbContext _context;

    public OrgUnitRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.OrgUnits.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid orgUnitId, CancellationToken cancellationToken = default)
    {
        return _context.OrgUnits.AnyAsync(x => x.Id == orgUnitId, cancellationToken);
    }

    public async Task AddAsync(OrgUnit orgUnit, CancellationToken cancellationToken = default)
    {
        await _context.OrgUnits.AddAsync(orgUnit, cancellationToken);
    }

    public Task<List<OrgUnit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.OrgUnits
            .AsNoTracking()
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}