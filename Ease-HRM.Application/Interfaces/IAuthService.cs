using Ease_HRM.Application.DTOs.Auth;

namespace Ease_HRM.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}