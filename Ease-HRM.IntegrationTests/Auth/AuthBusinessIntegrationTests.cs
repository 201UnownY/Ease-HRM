using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Auth;

public class AuthBusinessIntegrationTests
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
    public async Task EmployeesEndpoint_WithValidTokenAndPermission_ReturnsSuccess()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"employee-view-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAccessAsync(
            factory,
            email,
            password,
            Permissions.Employee.View);

        var token = await LoginAndGetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeDto>>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Empty(payload.Data!);
    }

    [Fact]
    public async Task EmployeesEndpoint_WithValidTokenButMissingPermission_Returns403()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"employee-no-access-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAccessAsync(factory, email, password);

        var token = await LoginAndGetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EmployeesEndpoint_WithoutToken_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SeedUserAccessAsync(
        ApiWebApplicationFactory factory,
        string email,
        string password,
        string? permissionName = null)
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
                var role = TestDataSeeder.SeedRole(db, $"auth-business-role-{Guid.NewGuid():N}");
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
