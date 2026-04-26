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

public class EmployeeApiTests
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
    public async Task CreateAndUpdateEmployee_WithAuthenticatedAuthorizedUser_Returns200_AndPersistsState()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var actorEmail = $"employee-api-actor-{Guid.NewGuid():N}@easehrm.test";
        var targetEmail = $"employee-api-target-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedEmployeeApiScenarioAsync(factory, actorEmail, targetEmail, password);

        var token = await LoginAndGetTokenAsync(client, actorEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest
        {
            UserId = seed.TargetUserId,
            FirstName = "Created",
            LastName = "Employee",
            Email = targetEmail,
            Phone = "1111111111",
            OrgUnitId = seed.OrgUnitId,
            ManagerId = null,
            JoinDate = DateTime.UtcNow.Date.AddDays(-30)
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        Assert.NotNull(createPayload);
        Assert.True(createPayload!.Success);
        Assert.NotNull(createPayload.Data);
        Assert.Equal("Created", createPayload.Data!.FirstName);
        Assert.False(string.IsNullOrWhiteSpace(createPayload.Data.RowVersion));

        var createdEmployee = createPayload.Data;

        var updateResponse = await client.PutAsJsonAsync("/api/employees/update", new UpdateEmployeeRequest
        {
            EmployeeId = createdEmployee.Id,
            FirstName = "Updated",
            LastName = "Employee",
            Email = targetEmail,
            Phone = "2222222222",
            JoinDate = createdEmployee.JoinDate,
            IsActive = true,
            RowVersion = createdEmployee.RowVersion
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        Assert.NotNull(updatePayload);
        Assert.True(updatePayload!.Success);
        Assert.NotNull(updatePayload.Data);
        Assert.Equal("Updated", updatePayload.Data!.FirstName);
        Assert.Equal("2222222222", updatePayload.Data.Phone);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var employee = await db.Employees.SingleAsync(x => x.Id == createdEmployee.Id);
            Assert.Equal("Updated", employee.FirstName);
            Assert.Equal("2222222222", employee.Phone);
            Assert.Equal(seed.TargetUserId, employee.UserId);
            Assert.NotEqual(createdEmployee.RowVersion, Convert.ToBase64String(employee.RowVersion));
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
                [Permissions.Employee.Create, Permissions.Employee.Update]);

            var targetUser = TestDataSeeder.SeedUser(db, targetEmail, "hash", isActive: true);
            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Employee Api Org {Guid.NewGuid():N}");

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
