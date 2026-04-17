using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class PayrollRepository : IPayrollRepository
{
    private readonly AppDbContext _context;

    public PayrollRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<SalaryStructure?> GetEffectiveSalaryAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        return _context.SalaryStructures
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId)
                .Where(x => x.EffectiveFrom <= targetDate &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= targetDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AttendanceSession>> GetAttendanceAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        return _context.AttendanceSessions
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Date >= start &&
                x.Date < end)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<AttendancePolicy?> GetEffectiveAttendancePolicyAsync(DateTime date, CancellationToken cancellationToken = default)
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

    public Task<List<LeaveRequest>> GetLeavesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endExclusive = startOfMonth.AddMonths(1);

        return _context.LeaveRequests
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status == LeaveStatus.Approved &&
                x.StartDate < endExclusive &&
                x.EndDate >= startOfMonth)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaveType>> GetLeaveTypesAsync(List<Guid> leaveTypeIds, CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes
            .AsNoTracking()
            .Where(x => leaveTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<List<Payroll>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Payrolls
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> PayrollExistsAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        return _context.Payrolls.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            x.Year == year &&
            x.Month == month,
            cancellationToken);
    }

    public Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(x => x.Id == employeeId, cancellationToken);
    }

    public async Task AddSalaryStructureAsync(SalaryStructure salaryStructure, CancellationToken cancellationToken = default)
    {
        await _context.SalaryStructures.AddAsync(salaryStructure, cancellationToken);
    }

    public Task<SalaryStructure?> GetSalaryStructureByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.SalaryStructures
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> HasOverlappingSalaryStructureAsync(
        Guid employeeId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeSalaryStructureId = null,
        CancellationToken cancellationToken = default)
    {
        var newStart = effectiveFrom.Date;
        var newEnd = (effectiveTo ?? DateTime.MaxValue).Date;

        return _context.SalaryStructures
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .Where(x => !excludeSalaryStructureId.HasValue || x.Id != excludeSalaryStructureId.Value)
            .AnyAsync(x => 
                x.EffectiveFrom <= newEnd && 
                (x.EffectiveTo ?? DateTime.MaxValue) >= newStart, 
                cancellationToken);
    }

    public async Task AddPayrollAsync(Payroll payroll, CancellationToken cancellationToken = default)
    {
        await _context.Payrolls.AddAsync(payroll, cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}