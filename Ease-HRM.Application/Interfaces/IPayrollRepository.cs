using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IPayrollRepository
{
    Task<SalaryStructure?> GetEffectiveSalaryAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<AttendanceSession>> GetAttendanceAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<AttendancePolicy?> GetEffectiveAttendancePolicyAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetLeavesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<List<LeaveType>> GetLeaveTypesAsync(List<Guid> leaveTypeIds, CancellationToken cancellationToken = default);
    Task<List<Payroll>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Payroll?> GetPayrollAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<bool> PayrollExistsAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task AddSalaryStructureAsync(SalaryStructure salaryStructure, CancellationToken cancellationToken = default);
    Task<SalaryStructure?> GetSalaryStructureByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingSalaryStructureAsync(Guid employeeId, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeSalaryStructureId = null, CancellationToken cancellationToken = default);
    Task AddPayrollAsync(Payroll payroll, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}