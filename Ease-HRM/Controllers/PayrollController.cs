using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Payroll;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("payroll")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HasPermission(Permissions.Payroll.ManageSalaryStructure)]
    [HttpPost("salary-structure")]
    public async Task<IActionResult> CreateSalaryStructure([FromBody] CreateSalaryStructureRequest request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.CreateSalaryStructureAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Salary structure created successfully"));
    }

    [HasPermission(Permissions.Payroll.ManageSalaryStructure)]
    [HttpPut("salary-structure")]
    public async Task<IActionResult> UpdateSalaryStructure([FromBody] UpdateSalaryStructureRequest request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.UpdateSalaryStructureAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Salary structure updated successfully"));
    }

    [HasPermission(Permissions.Payroll.Generate)]
    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePayroll([FromQuery] Guid employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await _payrollService.GeneratePayrollAsync(employeeId, year, month, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Payroll generated successfully"));
    }

    [HasPermission(Permissions.Payroll.View)]
    [HttpGet("{employeeId}")]
    public async Task<IActionResult> GetPayrolls(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _payrollService.GetPayrollsAsync(employeeId, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Payrolls fetched successfully"));
    }
}