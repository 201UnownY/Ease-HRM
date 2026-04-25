using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrgUnitRepository _orgUnitRepository;
    private readonly IExceptionTranslator _exceptionTranslator;

    public EmployeeService(IEmployeeRepository employeeRepository, IOrgUnitRepository orgUnitRepository, IExceptionTranslator exceptionTranslator)
    {
        _employeeRepository = employeeRepository;
        _orgUnitRepository = orgUnitRepository;
        _exceptionTranslator = exceptionTranslator;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = ValidationHelper.NormalizeEmail(request.Email);
        var firstName = ValidationHelper.RequireString(request.FirstName, "FirstName");
        var lastName = ValidationHelper.RequireString(request.LastName, "LastName");
        var phone = ValidationHelper.RequireString(request.Phone, "Phone");
        var userId = ValidationHelper.RequireGuid(request.UserId, "UserId");
        var orgUnitId = ValidationHelper.RequireGuid(request.OrgUnitId, "OrgUnitId");

        if (request.JoinDate > DateTime.UtcNow)
        {
            throw new ArgumentException("JoinDate cannot be in the future.");
        }

        if (request.ManagerId.HasValue && request.ManagerId != Guid.Empty)
        {
            if (request.ManagerId == userId)
            {
                throw new ArgumentException("Employee cannot be their own manager.");
            }

            if (!await _employeeRepository.ManagerExistsAsync(request.ManagerId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Manager not found.");
            }
        }

        if (!await _employeeRepository.UserExistsAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!await _orgUnitRepository.ExistsAsync(orgUnitId, cancellationToken))
        {
            throw new InvalidOperationException("OrgUnit not found.");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = normalizedEmail,
            Phone = phone,
            OrgUnitId = orgUnitId,
            ManagerId = request.ManagerId,
            JoinDate = request.JoinDate,
            IsActive = true
        };

        try
        {
            await _employeeRepository.AddAsync(employee, cancellationToken);
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Duplicate record detected.");
        }

        return new EmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            OrgUnitId = employee.OrgUnitId,
            ManagerId = employee.ManagerId,
            JoinDate = employee.JoinDate,
            IsActive = employee.IsActive
        };
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);

        return employees
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                UserId = x.UserId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Phone = x.Phone,
                OrgUnitId = x.OrgUnitId,
                ManagerId = x.ManagerId,
                JoinDate = x.JoinDate,
                IsActive = x.IsActive
            })
            .ToList()
            .AsReadOnly();
    }
}