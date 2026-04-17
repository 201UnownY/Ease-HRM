namespace Ease_HRM.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default);
}