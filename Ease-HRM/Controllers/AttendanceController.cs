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
[Route("attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HasPermission(Permissions.Attendance.CheckIn)]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken cancellationToken)
    {
        await _attendanceService.CheckInAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success<object?>(null, "Check-in successful"));
    }

    [HasPermission(Permissions.Attendance.CheckOut)]
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request, CancellationToken cancellationToken)
    {
        await _attendanceService.CheckOutAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success<object?>(null, "Check-out successful"));
    }

    [HasPermission(Permissions.Attendance.View)]
    [HttpGet]
    public async Task<IActionResult> GetAttendance(CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetAttendanceAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Attendance records fetched successfully"));
    }
}