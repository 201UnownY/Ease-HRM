using System.Security.Claims;
using Ease_HRM.Application.Interfaces;

namespace Ease_HRM.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return User.FindFirstValue(ClaimTypes.Email);
        }
    }

    public string? Role
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return User.FindFirstValue(ClaimTypes.Role);
        }
    }
}