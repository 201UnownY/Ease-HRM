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

    public Task<SalaryStructure?> GetActiveSalaryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.SalaryStructures
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AttendanceRecord>> GetAttendanceAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        return _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Date.Year == year &&
                x.Date.Month == month)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaveRequest>> GetLeavesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return _context.LeaveRequests
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.Status == LeaveStatus.Approved &&
                x.StartDate.Date <= endOfMonth &&
                x.EndDate.Date >= startOfMonth)
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

    public async Task DeactivateSalaryStructuresAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var active = await _context.SalaryStructures
            .Where(x => x.EmployeeId == employeeId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var salary in active)
        {
            salary.IsActive = false;
        }
    }

    public async Task AddPayrollAsync(Payroll payroll, CancellationToken cancellationToken = default)
    {
        await _context.Payrolls.AddAsync(payroll, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}