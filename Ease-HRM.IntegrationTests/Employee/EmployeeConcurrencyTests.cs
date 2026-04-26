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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Employee;

public class EmployeeConcurrencyTests
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
    public async Task UpdateEmployee_WithStaleRowVersion_Returns409_AndPreservesLatestState()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var actorEmail = $"employee-concurrency-actor-{Guid.NewGuid():N}@easehrm.test";
        var targetEmail = $"employee-concurrency-target-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedEmployeeApiScenarioAsync(factory, actorEmail, targetEmail, password);

        var token = await LoginAndGetTokenAsync(client, actorEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest
        {
            UserId = seed.TargetUserId,
            FirstName = "Initial",
            LastName = "Employee",
            Email = targetEmail,
            Phone = "3333333333",
            OrgUnitId = seed.OrgUnitId,
            JoinDate = DateTime.UtcNow.Date.AddDays(-30)
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        Assert.NotNull(createPayload);
        Assert.NotNull(createPayload!.Data);

        var createdEmployee = createPayload.Data!;
        var fetchResponse = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);

        var fetchPayload = await fetchResponse.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeDto>>>(JsonOptions);
        Assert.NotNull(fetchPayload);
        Assert.NotNull(fetchPayload!.Data);

        var fetchedEmployee = fetchPayload.Data!.Single(x => x.Id == createdEmployee.Id);

        var firstUpdate = new UpdateEmployeeRequest
        {
            EmployeeId = fetchedEmployee.Id,
            FirstName = "Latest",
            LastName = "Employee",
            Email = targetEmail,
            Phone = "4444444444",
            JoinDate = fetchedEmployee.JoinDate,
            IsActive = true,
            RowVersion = fetchedEmployee.RowVersion
        };

        var firstUpdateResponse = await client.PutAsJsonAsync("/api/employees/update", firstUpdate);
        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        var staleUpdateResponse = await client.PutAsJsonAsync("/api/employees/update", new UpdateEmployeeRequest
        {
            EmployeeId = fetchedEmployee.Id,
            FirstName = "Stale",
            LastName = "Employee",
            Email = targetEmail,
            Phone = "5555555555",
            JoinDate = fetchedEmployee.JoinDate,
            IsActive = true,
            RowVersion = fetchedEmployee.RowVersion
        });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees.SingleAsync(x => x.Id == createdEmployee.Id);
            Assert.Equal("Latest", employee.FirstName);
            Assert.Equal("4444444444", employee.Phone);
        });
    }

    private static async Task<EmployeeApiSeedData> SeedEmployeeApiScenarioAsync(
        ApiWebApplicationFactory factory,
        string actorEmail,
        string targetEmail,
        string password)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        var seed = new EmployeeApiSeedData(Guid.Empty, Guid.Empty);

        await factory.ExecuteDbContextAsync(async db =>
        {
            _ = await TestDataSeeder.SeedUserWithPermissionsAsync(
                db,
                actorEmail,
                passwordHash,
                [Permissions.Employee.Create, Permissions.Employee.Update, Permissions.Employee.View]);

            var targetUser = TestDataSeeder.SeedUser(db, targetEmail, "hash", isActive: true);
            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Employee Concurrency Org {Guid.NewGuid():N}");

            await db.SaveChangesAsync();

            seed = new EmployeeApiSeedData(targetUser.Id, orgUnit.Id);
        });

        return seed;
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
    private sealed record EmployeeApiSeedData(Guid TargetUserId, Guid OrgUnitId);
}
