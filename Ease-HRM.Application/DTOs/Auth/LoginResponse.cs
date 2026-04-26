namespace Ease_HRM.Application.DTOs.Auth;

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string Email,
    string Role);