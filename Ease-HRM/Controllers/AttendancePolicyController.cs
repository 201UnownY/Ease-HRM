using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Attendance;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("attendance-policy")]
public class AttendancePolicyController : ControllerBase
{
    private readonly IAttendancePolicyService _attendancePolicyService;

    public AttendancePolicyController(IAttendancePolicyService attendancePolicyService)
    {
        _attendancePolicyService = attendancePolicyService;
    }

    [HasPermission(Permissions.Attendance.ManagePolicy)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttendancePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _attendancePolicyService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Attendance policy created successfully"));
    }

    [HasPermission(Permissions.Attendance.ManagePolicy)]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAttendancePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _attendancePolicyService.UpdateAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Attendance policy updated successfully"));
    }
}
