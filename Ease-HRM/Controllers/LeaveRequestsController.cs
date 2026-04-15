using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    [HasPermission(Permissions.Leave.Apply)]
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.ApplyLeaveAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave request applied successfully"));
    }

    [HasPermission(Permissions.Leave.Approve)]
    [HttpPost("approve")]
    public async Task<IActionResult> ApproveLeave([FromBody] ApproveLeaveRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.ApproveLeaveAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave request approved successfully"));
    }

    [HasPermission(Permissions.Leave.Reject)]
    [HttpPost("reject")]
    public async Task<IActionResult> RejectLeave([FromBody] RejectLeaveRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.RejectLeaveAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave request rejected successfully"));
    }

    [HasPermission(Permissions.Leave.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.GetAllLeaveRequestsAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Leave requests fetched successfully"));
    }
}