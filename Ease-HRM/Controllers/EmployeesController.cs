using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HasPermission(Permissions.Employee.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Employee created successfully"));
    }

    [HasPermission(Permissions.Employee.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetAllEmployeesAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Employees fetched successfully"));
    }

    [HasPermission(Permissions.Employee.Update)]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.UpdateEmployeeAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Employee updated successfully"));
    }

    [HasPermission(Permissions.Employee.Update)]
    [HttpPut("change-manager")]
    public async Task<IActionResult> ChangeManager([FromBody] ChangeManagerRequest request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.ChangeManagerAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Employee manager updated successfully"));
    }

    [HasPermission(Permissions.Employee.Update)]
    [HttpPut("update-org-unit")]
    public async Task<IActionResult> UpdateOrgUnit([FromBody] UpdateEmployeeOrgUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.UpdateOrgUnitAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Employee org unit updated successfully"));
    }
}

