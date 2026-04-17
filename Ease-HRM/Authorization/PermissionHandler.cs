using Ease_HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ease_HRM.Api.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string PermissionsCacheKeyPrefix = "permissions:";
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public PermissionHandler(ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(requirement.Permission))
        {
            return;
        }

        var permissions = await GetOrLoadPermissionsAsync(context, userId.Value, CancellationToken.None);
        var hasPermission = permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }

    private async Task<IReadOnlyCollection<string>> GetOrLoadPermissionsAsync(AuthorizationHandlerContext context, Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{PermissionsCacheKeyPrefix}{userId}";
        var httpContext = GetHttpContext(context);

        if (httpContext is not null && httpContext.Items.TryGetValue(cacheKey, out var cached) && cached is IReadOnlyCollection<string> cachedPermissions)
        {
            return cachedPermissions;
        }

        var normalized = await _permissionService.GetPermissionsAsync(userId, cancellationToken);

        if (httpContext is not null)
        {
            httpContext.Items[cacheKey] = normalized;
        }

        return normalized;
    }

    private static HttpContext? GetHttpContext(AuthorizationHandlerContext context)
    {
        if (context.Resource is HttpContext httpContext)
        {
            return httpContext;
        }

        if (context.Resource is AuthorizationFilterContext mvcContext)
        {
            return mvcContext.HttpContext;
        }

        return null;
    }
}