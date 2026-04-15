using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Ease_HRM.Api.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public PermissionHandler(ICurrentUserService currentUserService, IRolePermissionRepository rolePermissionRepository)
    {
        _currentUserService = currentUserService;
        _rolePermissionRepository = rolePermissionRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var role = _currentUserService.Role;
        if (string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(requirement.Permission))
        {
            return;
        }

        var hasPermission = await _rolePermissionRepository.RoleHasPermissionAsync(role, requirement.Permission, CancellationToken.None);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}