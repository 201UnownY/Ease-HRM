using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.RolePermissions;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("role-permissions")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolePermissionsController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    [HasPermission(Permissions.RolePermission.Assign)]
    [HttpPost]
    public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.AssignPermissionToRoleAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Permission assigned to role successfully"));
    }

    [HasPermission(Permissions.RolePermission.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _rolePermissionService.GetAllAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Role-permission mappings fetched successfully"));
    }
}