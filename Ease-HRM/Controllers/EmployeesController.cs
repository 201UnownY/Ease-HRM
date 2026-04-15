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
[Route("employees")]
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
}