using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;

    public AttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AttendancePolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return _context.AttendancePolicies
            .FirstOrDefaultAsync(x => x.Id == policyId, cancellationToken);
    }

    public Task<bool> HasOverlappingPolicyAsync(DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludePolicyId = null, CancellationToken cancellationToken = default)
    {
        var newStart = effectiveFrom.Date;
        var newEnd = (effectiveTo ?? DateTime.MaxValue).Date;

        return _context.AttendancePolicies
            .AsNoTracking()
            .Where(x => !excludePolicyId.HasValue || x.Id != excludePolicyId.Value)
            .AnyAsync(x =>
                x.EffectiveFrom <= newEnd &&
                (x.EffectiveTo ?? DateTime.MaxValue) >= newStart,
                cancellationToken);
    }

    public async Task AddPolicyAsync(AttendancePolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.AttendancePolicies.AddAsync(policy, cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(x => x.Id == employeeId, cancellationToken);
    }

    public Task<AttendanceSession?> GetActiveSessionAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.AttendanceSessions
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.CheckOutTime == null, cancellationToken);
    }

    public Task<List<AttendanceSession>> GetSessionsByDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return _context.AttendanceSessions
            .Where(x => x.EmployeeId == employeeId && x.Date >= start && x.Date < end)
            .OrderBy(x => x.CheckInTime)
            .ToListAsync(cancellationToken);
    }

    public Task<AttendancePolicy?> GetEffectivePolicyAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        return _context.AttendancePolicies
            .AsNoTracking()
            .Where(x =>
                x.EffectiveFrom <= targetDate &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= targetDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasApprovedLeaveAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dayStart = date.Date;
        var dayEndExclusive = dayStart.AddDays(1);

        return _context.LeaveRequests.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.Status == LeaveStatus.Approved &&
            x.StartDate < dayEndExclusive &&
            x.EndDate >= dayStart,
            cancellationToken);
    }

    public async Task AddSessionAsync(AttendanceSession session, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceSessions.AddAsync(session, cancellationToken);
    }

    public Task<List<AttendanceSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendanceSessions
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(Guid EmployeeId, DateTime Start, DateTime End)>> GetApprovedLeaveRangesAsync(CancellationToken cancellationToken = default)
    {
        var approvedLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.Status == LeaveStatus.Approved)
            .Select(x => new { x.EmployeeId, x.StartDate, x.EndDate })
            .ToListAsync(cancellationToken);

        return approvedLeaves
            .Select(x => (x.EmployeeId, x.StartDate.Date, x.EndDate.Date))
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}