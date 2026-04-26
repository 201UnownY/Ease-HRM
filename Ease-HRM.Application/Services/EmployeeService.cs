using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private const string ConcurrencyConflictMessage = "The record was modified by another user. Please refresh and try again.";

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrgUnitRepository _orgUnitRepository;
    private readonly IExceptionTranslator _exceptionTranslator;

    public EmployeeService(IEmployeeRepository employeeRepository, IUserRepository userRepository, IOrgUnitRepository orgUnitRepository, IExceptionTranslator exceptionTranslator)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
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
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

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

        if (!await _orgUnitRepository.ExistsAsync(orgUnitId, cancellationToken))
        {
            throw new InvalidOperationException("OrgUnit not found.");
        }

        var normalizedUserEmail = ValidationHelper.NormalizeEmail(user.Email);
        if (!string.Equals(normalizedUserEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Employee email must match the linked user email.");
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

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, nameof(request.EmployeeId));
        var rowVersion = ValidationHelper.RequireRowVersion(request.RowVersion);
        var firstName = ValidationHelper.RequireString(request.FirstName, nameof(request.FirstName));
        var lastName = ValidationHelper.RequireString(request.LastName, nameof(request.LastName));
        var phone = ValidationHelper.RequireString(request.Phone, nameof(request.Phone));
        var email = ValidationHelper.NormalizeEmail(request.Email);

        if (request.JoinDate > DateTime.UtcNow)
        {
            throw new ArgumentException("JoinDate cannot be in the future.");
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var user = await _userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var normalizedUserEmail = ValidationHelper.NormalizeEmail(user.Email);
        if (!string.Equals(normalizedUserEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Employee email must match the linked user email.");
        }

        _employeeRepository.SetOriginalRowVersion(employee, rowVersion);

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.Email = email;
        employee.Phone = phone;
        employee.JoinDate = request.JoinDate;
        employee.IsActive = request.IsActive;

        try
        {
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Duplicate record detected.");
        }
        catch (Exception ex) when (_exceptionTranslator.IsConcurrencyConflict(ex))
        {
            throw new ConcurrencyException(ConcurrencyConflictMessage, nameof(Employee));
        }

        return ToDto(employee);
    }

    public async Task<EmployeeDto> ChangeManagerAsync(ChangeManagerRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, nameof(request.EmployeeId));
        var rowVersion = ValidationHelper.RequireRowVersion(request.RowVersion);

        if (request.ManagerId.HasValue && request.ManagerId != Guid.Empty)
        {
            if (request.ManagerId.Value == employeeId)
            {
                throw new ArgumentException("Employee cannot be their own manager.");
            }

            if (!await _employeeRepository.ManagerExistsAsync(request.ManagerId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Manager not found.");
            }
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        _employeeRepository.SetOriginalRowVersion(employee, rowVersion);

        employee.ManagerId = request.ManagerId;

        try
        {
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsConcurrencyConflict(ex))
        {
            throw new ConcurrencyException(ConcurrencyConflictMessage, nameof(Employee));
        }

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateOrgUnitAsync(UpdateEmployeeOrgUnitRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, nameof(request.EmployeeId));
        var orgUnitId = ValidationHelper.RequireGuid(request.OrgUnitId, nameof(request.OrgUnitId));
        var rowVersion = ValidationHelper.RequireRowVersion(request.RowVersion);

        if (!await _orgUnitRepository.ExistsAsync(orgUnitId, cancellationToken))
        {
            throw new InvalidOperationException("OrgUnit not found.");
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        _employeeRepository.SetOriginalRowVersion(employee, rowVersion);

        employee.OrgUnitId = orgUnitId;

        try
        {
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsConcurrencyConflict(ex))
        {
            throw new ConcurrencyException(ConcurrencyConflictMessage, nameof(Employee));
        }

        return ToDto(employee);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);

        return employees
            .Select(ToDto)
            .ToList()
            .AsReadOnly();
    }

    private static EmployeeDto ToDto(Employee employee)
    {
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
            IsActive = employee.IsActive,
            RowVersion = employee.RowVersion is { Length: > 0 }
                ? Convert.ToBase64String(employee.RowVersion)
                : string.Empty
        };
    }
}