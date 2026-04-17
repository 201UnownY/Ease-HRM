using Ease_HRM.Application.DTOs.Payroll;

namespace Ease_HRM.Application.Interfaces;

public interface IPayrollService
{
    Task<SalaryStructureDto> CreateSalaryStructureAsync(CreateSalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<SalaryStructureDto> UpdateSalaryStructureAsync(UpdateSalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<PayrollDto> GeneratePayrollAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollDto>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}