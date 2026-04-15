using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests
            .AsNoTracking()
            .OrderByDescending(x => x.AppliedOn)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(x => x.Id == employeeId, cancellationToken);
    }

    public Task<bool> LeaveTypeExistsAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes.AnyAsync(x => x.Id == leaveTypeId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}