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
        return _context.AttendanceSessions
            .Where(x => x.EmployeeId == employeeId && x.Date == date.Date)
            .OrderBy(x => x.CheckInTime)
            .ToListAsync(cancellationToken);
    }

    public Task<AttendanceRecord?> GetRecordByDateAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        return _context.AttendanceRecords
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Date == date.Date, cancellationToken);
    }

    public Task<AttendancePolicy?> GetActivePolicyAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendancePolicies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasApprovedLeaveAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.Status == LeaveStatus.Approved &&
            x.StartDate.Date <= date.Date &&
            x.EndDate.Date >= date.Date,
            cancellationToken);
    }

    public async Task AddSessionAsync(AttendanceSession session, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceSessions.AddAsync(session, cancellationToken);
    }

    public async Task AddRecordAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceRecords.AddAsync(record, cancellationToken);
    }

    public Task<List<AttendanceRecord>> GetAllRecordsAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendanceRecords
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}