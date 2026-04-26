using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Auth;

public class AuthorizationTests
{
    private static readonly WebApplicationFactoryClientOptions ClientOptions = new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidTokenButMissingPermission_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"no-permission-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAndAccessAsync(factory, email, password, grantPermission: false);

        var token = await LoginAndGetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidPermission_Returns200()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"with-permission-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAndAccessAsync(factory, email, password, grantPermission: true);

        var token = await LoginAndGetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Authorization_WithWrongPermission_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"wrong-permission-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAndAccessAsync(factory, email, password, Permissions.Employee.View);

        var token = await LoginAndGetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/employees", new
        {
            userId = Guid.NewGuid(),
            firstName = "Test",
            lastName = "User",
            email = $"new-employee-{Guid.NewGuid():N}@easehrm.test",
            phone = "0000000000",
            orgUnitId = Guid.NewGuid(),
            managerId = (Guid?)null,
            joinDate = DateTime.UtcNow.Date
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task SeedUserAndAccessAsync(ApiWebApplicationFactory factory, string email, string password, bool grantPermission)
    {
        await SeedUserAndAccessAsync(
            factory,
            email,
            password,
            grantPermission ? Permissions.Employee.View : null);
    }

    private static async Task SeedUserAndAccessAsync(
        ApiWebApplicationFactory factory,
        string email,
        string password,
        string? permissionName)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        await factory.ExecuteDbContextAsync(async db =>
        {
            if (!string.IsNullOrWhiteSpace(permissionName))
            {
                _ = await TestDataSeeder.SeedUserWithPermissionAsync(db, email, passwordHash, permissionName);
            }
            else
            {
                var user = TestDataSeeder.SeedUser(db, email, passwordHash, isActive: true);
                var role = TestDataSeeder.SeedRole(db, $"authz-role-{Guid.NewGuid():N}");
                TestDataSeeder.SeedUserRole(db, user.Id, role.Id);
            }

            await db.SaveChangesAsync();
        });
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));

        return payload.Token;
    }

    private sealed record LoginResponsePayload(string Token, DateTime ExpiresAt, string Email, string Role);
}
