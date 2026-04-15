namespace Ease_HRM.Application.DTOs.Users;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
}