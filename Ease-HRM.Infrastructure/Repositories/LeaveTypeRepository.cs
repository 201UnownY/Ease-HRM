using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly AppDbContext _context;

    public LeaveTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        await _context.LeaveTypes.AddAsync(leaveType, cancellationToken);
    }

    public Task<List<LeaveType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}