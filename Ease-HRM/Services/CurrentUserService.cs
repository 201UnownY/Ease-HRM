using System.Security.Claims;
using Ease_HRM.Application.Interfaces;

namespace Ease_HRM.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRoleRepository _userRoleRepository;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRoleRepository userRoleRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRoleRepository = userRoleRepository;
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

    public async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var userId = UserId;
        if (!userId.HasValue)
        {
            return Array.Empty<string>();
        }

        var roleNames = await _userRoleRepository.GetUserRoleNamesAsync(userId.Value, cancellationToken);

        return roleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}