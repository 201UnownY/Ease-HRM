using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IPayrollRepository
{
    Task<SalaryStructure?> GetActiveSalaryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<AttendanceRecord>> GetAttendanceAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetLeavesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<List<LeaveType>> GetLeaveTypesAsync(List<Guid> leaveTypeIds, CancellationToken cancellationToken = default);
    Task<List<Payroll>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> PayrollExistsAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task AddSalaryStructureAsync(SalaryStructure salaryStructure, CancellationToken cancellationToken = default);
    Task DeactivateSalaryStructuresAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task AddPayrollAsync(Payroll payroll, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}