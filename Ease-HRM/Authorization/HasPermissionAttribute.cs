using Microsoft.AspNetCore.Authorization;

namespace Ease_HRM.Api.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = permission;
    }
}