using Ease_HRM.Application.DTOs.Employees;

namespace Ease_HRM.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> ChangeManagerAsync(ChangeManagerRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateOrgUnitAsync(UpdateEmployeeOrgUnitRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
}