using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.LeaveTypes;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("leave-types")]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HasPermission(Permissions.LeaveType.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveTypeService.CreateLeaveTypeAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave type created successfully"));
    }

    [HasPermission(Permissions.LeaveType.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _leaveTypeService.GetAllLeaveTypesAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave types fetched successfully"));
    }
}