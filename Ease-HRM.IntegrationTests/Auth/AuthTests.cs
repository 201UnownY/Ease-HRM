using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Auth;

public class AuthTests
{
    private static readonly TimeSpan ExpectedTokenLifetime = TimeSpan.FromHours(8);
    private static readonly TimeSpan ExpirationTolerance = TimeSpan.FromSeconds(5);

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
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"valid-login-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAsync(factory, email, password, isActive: true, includeRole: true);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponsePayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"invalid-password-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAsync(factory, email, password, isActive: true, includeRole: true);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongP@ssw0rd"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@easehrm.test",
            password = "AnyP@ssw0rd"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"inactive-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAsync(factory, email, password, isActive: false, includeRole: true);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnedToken_HasExpectedClaims_AndExpiration()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var email = $"token-structure-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        await SeedUserAsync(factory, email, password, isActive: true, includeRole: true, includePermissionClaim: true);

        var requestStartedAt = DateTime.UtcNow;
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        var requestCompletedAt = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponsePayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(payload.Token);

        var userIdClaim = jwt.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.True(Guid.TryParse(userIdClaim!.Value, out _));

        var emailClaim = jwt.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim!.Value);

        Assert.Contains(jwt.Claims, c =>
            c.Type == "permission" &&
            !string.IsNullOrWhiteSpace(c.Value));

        var actualLifetime = jwt.ValidTo - jwt.ValidFrom;
        Assert.InRange(
            actualLifetime,
            ExpectedTokenLifetime - ExpirationTolerance,
            ExpectedTokenLifetime + ExpirationTolerance);

        var expectedEarliestExpiration = requestStartedAt.Add(ExpectedTokenLifetime).Subtract(ExpirationTolerance);
        var expectedLatestExpiration = requestCompletedAt.Add(ExpectedTokenLifetime).Add(ExpirationTolerance);

        Assert.InRange(jwt.ValidTo, expectedEarliestExpiration, expectedLatestExpiration);
        Assert.InRange(payload.ExpiresAt, expectedEarliestExpiration, expectedLatestExpiration);

        var expirationDifference = (payload.ExpiresAt - jwt.ValidTo).Duration();
        Assert.True(
            expirationDifference <= ExpirationTolerance,
            $"Expected payload expiration to match JWT expiration within {ExpirationTolerance}, but the difference was {expirationDifference}.");
    }

    private static async Task SeedUserAsync(
        ApiWebApplicationFactory factory,
        string email,
        string password,
        bool isActive,
        bool includeRole,
        bool includePermissionClaim = false)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        await factory.ExecuteDbContextAsync(async db =>
        {
            if (!isActive)
            {
                _ = TestDataSeeder.SeedUser(db, email, passwordHash, isActive: false);
            }
            else if (includePermissionClaim)
            {
                _ = await TestDataSeeder.SeedUserWithPermissionAsync(db, email, passwordHash, Permissions.Permission.View);
            }
            else if (includeRole)
            {
                var user = TestDataSeeder.SeedUser(db, email, passwordHash, isActive: true);
                var role = TestDataSeeder.SeedRole(db, $"role-{Guid.NewGuid():N}");
                TestDataSeeder.SeedUserRole(db, user.Id, role.Id);
            }
            else
            {
                _ = TestDataSeeder.SeedUser(db, email, passwordHash, isActive: true);
            }

            await db.SaveChangesAsync();
        });
    }

    private sealed record LoginResponsePayload(string Token, DateTime ExpiresAt, string Email, string Role);
}
