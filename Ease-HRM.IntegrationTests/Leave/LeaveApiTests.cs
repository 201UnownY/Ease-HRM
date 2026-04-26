using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Domain.Enums;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveApiTests
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
    public async Task ApplyAndApproveLeave_WithAuthenticatedUsers_UpdatesStatusAndBalance()
    {
        await using var factory = new ApiWebApplicationFactory();
        var employeeClient = factory.CreateClient(ClientOptions);
        var managerClient = factory.CreateClient(ClientOptions);

        var employeeEmail = $"leave-api-employee-{Guid.NewGuid():N}@easehrm.test";
        var managerEmail = $"leave-api-manager-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedLeaveScenarioAsync(factory, employeeEmail, managerEmail, password);

        var employeeToken = await LoginAndGetTokenAsync(employeeClient, employeeEmail, password);
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var startDate = DateTime.UtcNow.Date.AddDays(10);
        var endDate = startDate.AddDays(1);

        var applyResponse = await employeeClient.PostAsJsonAsync("/api/leaveRequests/apply", new ApplyLeaveRequest
        {
            EmployeeId = seed.EmployeeId,
            LeaveTypeId = seed.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Family event"
        });

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        var applyPayload = await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(applyPayload);
        Assert.True(applyPayload!.Success);
        Assert.NotNull(applyPayload.Data);
        Assert.Equal("Pending", applyPayload.Data!.Status);

        var managerToken = await LoginAndGetTokenAsync(managerClient, managerEmail, password);
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var approveResponse = await managerClient.PostAsJsonAsync("/api/leaveRequests/approve", new ApproveLeaveRequest
        {
            LeaveRequestId = applyPayload.Data.Id,
            RowVersion = applyPayload.Data.RowVersion
        });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvePayload = await approveResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(approvePayload);
        Assert.True(approvePayload!.Success);
        Assert.NotNull(approvePayload.Data);
        Assert.Equal("Approved", approvePayload.Data!.Status);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var request = await db.LeaveRequests.SingleAsync(x => x.Id == applyPayload.Data.Id);
            var balance = await db.LeaveBalances.SingleAsync(x =>
                x.EmployeeId == seed.EmployeeId &&
                x.LeaveTypeId == seed.LeaveTypeId &&
                x.Year == startDate.Year);

            Assert.Equal(LeaveStatus.Approved, request.Status);
            Assert.Equal(2m, balance.Used);
        });
    }

    private static async Task<LeaveApiSeedData> SeedLeaveScenarioAsync(
        ApiWebApplicationFactory factory,
        string employeeEmail,
        string managerEmail,
        string password)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        var seed = new LeaveApiSeedData(Guid.Empty, Guid.Empty);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var employeeUser = await TestDataSeeder.SeedUserWithPermissionAsync(db, employeeEmail, passwordHash, Permissions.Leave.Apply);
            var managerUser = await TestDataSeeder.SeedUserWithPermissionAsync(db, managerEmail, passwordHash, Permissions.Leave.Approve);

            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Leave Api Org {Guid.NewGuid():N}");
            var manager = TestDataSeeder.SeedEmployee(db, managerUser.Id, orgUnit.Id, null, managerEmail);
            var employee = TestDataSeeder.SeedEmployee(db, employeeUser.Id, orgUnit.Id, manager.Id, employeeEmail);
            var leaveType = TestDataSeeder.SeedLeaveType(db, $"Leave Api Type {Guid.NewGuid():N}", isPaid: false, weight: 1m);

            _ = TestDataSeeder.SeedLeaveBalance(db, employee.Id, leaveType.Id, DateTime.UtcNow.Year, allocated: 10m, used: 0m);

            await db.SaveChangesAsync();

            seed = new LeaveApiSeedData(employee.Id, leaveType.Id);
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
    private sealed record LeaveApiSeedData(Guid EmployeeId, Guid LeaveTypeId);
}
