using Ease_HRM.Application.Interfaces;

namespace Ease_HRM.IntegrationTests.TestInfrastructure;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    private readonly IReadOnlyList<string> _roles;

    public TestCurrentUserService(Guid? userId, string? email, IReadOnlyList<string>? roles = null)
    {
        UserId = userId;
        Email = email;
        _roles = roles ?? [];
    }

    public Guid? UserId { get; }
    public string? Email { get; }

    public Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_roles);
}
