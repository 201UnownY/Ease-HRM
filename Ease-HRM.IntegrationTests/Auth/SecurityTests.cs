using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ease_HRM.IntegrationTests.Auth;

public class SecurityTests
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
    public async Task ProtectedEndpoint_WithManuallyModifiedToken_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"tampered-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAccessAsync(factory, email, password, Permissions.Employee.View);

        var validToken = await LoginAndGetTokenAsync(client, email, password);
        var modifiedToken = TamperToken(validToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", modifiedToken);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var expiredToken = CreateExpiredToken(factory.Services.GetRequiredService<IConfiguration>());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_Expires_AfterLogin_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory(TimeSpan.FromSeconds(5));
        var client = factory.CreateClient(ClientOptions);

        var email = $"expiring-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAccessAsync(factory, email, password, Permissions.Employee.View);

        var loginPayload = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Token);

        var delay = loginPayload.ExpiresAt - DateTime.UtcNow + TimeSpan.FromSeconds(1);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay);
        }

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SeedUserAccessAsync(
        ApiWebApplicationFactory factory,
        string email,
        string password,
        string permissionName)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        await factory.ExecuteDbContextAsync(async db =>
        {
            _ = await TestDataSeeder.SeedUserWithPermissionAsync(db, email, passwordHash, permissionName);
            await db.SaveChangesAsync();
        });
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var payload = await LoginAsync(client, email, password);
        return payload.Token;
    }

    private static async Task<LoginResponsePayload> LoginAsync(HttpClient client, string email, string password)
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

        return payload;
    }

    private static string TamperToken(string token)
    {
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        parts[2] = "invalidsignature";

        return string.Join(".", parts);
    }

    private static string CreateExpiredToken(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, $"expired-{Guid.NewGuid():N}@easehrm.test"),
            new("permission", Permissions.Employee.View)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record LoginResponsePayload(string Token, DateTime ExpiresAt, string Email, string Role);
}
