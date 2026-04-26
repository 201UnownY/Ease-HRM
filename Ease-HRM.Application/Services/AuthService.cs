using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.Auth;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;

namespace Ease_HRM.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IPermissionService _permissionService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IPermissionService permissionService,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _permissionService = permissionService;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = ValidationHelper.NormalizeEmail(request.Email);
        var password = ValidationHelper.RequireString(request.Password, nameof(request.Password));

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roleNames = await _userRoleRepository.GetUserRoleNamesAsync(user.Id, cancellationToken);
        var permissions = await _permissionService.GetPermissionsAsync(user.Id, cancellationToken);
        var token = _tokenService.GenerateToken(user, permissions);

        return new LoginResponse(
            token,
            _tokenService.GetExpirationUtc(),
            user.Email,
            roleNames.FirstOrDefault() ?? string.Empty);
    }
}