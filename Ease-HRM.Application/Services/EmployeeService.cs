using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrgUnitRepository _orgUnitRepository;

    public EmployeeService(IEmployeeRepository employeeRepository, IOrgUnitRepository orgUnitRepository)
    {
        _employeeRepository = employeeRepository;
        _orgUnitRepository = orgUnitRepository;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new ArgumentException("FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("LastName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new ArgumentException("Phone is required.");
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (request.OrgUnitId == Guid.Empty)
        {
            throw new ArgumentException("OrgUnitId is required.");
        }

        if (request.JoinDate > DateTime.UtcNow)
        {
            throw new ArgumentException("JoinDate cannot be in the future.");
        }

        if (request.ManagerId.HasValue && request.ManagerId != Guid.Empty)
        {
            if (request.ManagerId == request.UserId)
            {
                throw new ArgumentException("Employee cannot be their own manager.");
            }

            if (!await _employeeRepository.ManagerExistsAsync(request.ManagerId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Manager not found.");
            }
        }

        if (await _employeeRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        if (!await _employeeRepository.UserExistsAsync(request.UserId, cancellationToken))
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!await _orgUnitRepository.ExistsAsync(request.OrgUnitId, cancellationToken))
        {
            throw new InvalidOperationException("OrgUnit not found.");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            Phone = request.Phone.Trim(),
            OrgUnitId = request.OrgUnitId,
            ManagerId = request.ManagerId,
            JoinDate = request.JoinDate,
            IsActive = true
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

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