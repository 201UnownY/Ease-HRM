using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Permissions;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HasPermission(Permissions.Permission.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await _permissionService.CreatePermissionAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Permission created successfully"));
    }

    [HasPermission(Permissions.Permission.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetAllPermissionsAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Permissions fetched successfully"));
    }
}