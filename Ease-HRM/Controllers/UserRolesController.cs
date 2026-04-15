using Ease_HRM.Api.Authorization;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.UserRoles;
using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ease_HRM.Api.Controllers;

[Authorize]
[ApiController]
[Route("user-roles")]
public class UserRolesController : ControllerBase
{
    private readonly IUserRoleService _userRoleService;

    public UserRolesController(IUserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }

    [HasPermission(Permissions.UserRole.Assign)]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _userRoleService.AssignRoleToUserAsync(request, cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "Role assigned successfully"));
    }

    [HasPermission(Permissions.UserRole.View)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userRoleService.GetAllAsync(cancellationToken);
        return Ok(ApiResponseHelper.Success(result, "User-role mappings fetched successfully"));
    }
}