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

public class LeaveConcurrencyApiTests
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
    public async Task ApproveLeave_WithStaleRowVersion_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();
        var employeeClient = factory.CreateClient(ClientOptions);
        var managerClient = factory.CreateClient(ClientOptions);

        var employeeEmail = $"leave-concurrency-employee-{Guid.NewGuid():N}@easehrm.test";
        var managerEmail = $"leave-concurrency-manager-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedLeaveScenarioAsync(factory, employeeEmail, managerEmail, password);

        var employeeToken = await LoginAndGetTokenAsync(employeeClient, employeeEmail, password);
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var startDate = DateTime.UtcNow.Date.AddDays(14);
        var endDate = startDate.AddDays(1);

        var applyResponse = await employeeClient.PostAsJsonAsync("/api/leaveRequests/apply", new ApplyLeaveRequest
        {
            EmployeeId = seed.EmployeeId,
            LeaveTypeId = seed.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Concurrency approve"
        });

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        var applyPayload = await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(applyPayload);
        Assert.NotNull(applyPayload!.Data);

        var managerToken = await LoginAndGetTokenAsync(managerClient, managerEmail, password);
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var fetchResponse = await managerClient.GetAsync("/api/leaveRequests");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);

        var fetchPayload = await fetchResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveRequestApiModel>>>(JsonOptions);
        Assert.NotNull(fetchPayload);
        Assert.NotNull(fetchPayload!.Data);

        var fetchedLeave = fetchPayload.Data!.Single(x => x.Id == applyPayload.Data!.Id);
        var rowVersion = Convert.FromBase64String(fetchedLeave.RowVersion);

        var approveResponse = await managerClient.PostAsJsonAsync("/api/leaveRequests/approve", new ApproveLeaveRequest
        {
            LeaveRequestId = fetchedLeave.Id,
            RowVersion = rowVersion
        });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var staleApproveResponse = await managerClient.PostAsJsonAsync("/api/leaveRequests/approve", new ApproveLeaveRequest
        {
            LeaveRequestId = fetchedLeave.Id,
            RowVersion = rowVersion
        });

        Assert.Equal(HttpStatusCode.Conflict, staleApproveResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var leave = await db.LeaveRequests.SingleAsync(x => x.Id == fetchedLeave.Id);
            Assert.Equal(LeaveStatus.Approved, leave.Status);
        });
    }

    [Fact]
    public async Task RejectLeave_WithStaleRowVersion_Returns409()
    {
        await using var factory = new ApiWebApplicationFactory();
        var employeeClient = factory.CreateClient(ClientOptions);
        var managerClient = factory.CreateClient(ClientOptions);

        var employeeEmail = $"leave-concurrency-reject-employee-{Guid.NewGuid():N}@easehrm.test";
        var managerEmail = $"leave-concurrency-reject-manager-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedLeaveScenarioAsync(factory, employeeEmail, managerEmail, password);

        var employeeToken = await LoginAndGetTokenAsync(employeeClient, employeeEmail, password);
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var startDate = DateTime.UtcNow.Date.AddDays(21);
        var endDate = startDate.AddDays(1);

        var applyResponse = await employeeClient.PostAsJsonAsync("/api/leaveRequests/apply", new ApplyLeaveRequest
        {
            EmployeeId = seed.EmployeeId,
            LeaveTypeId = seed.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Concurrency reject"
        });

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        var applyPayload = await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(applyPayload);
        Assert.NotNull(applyPayload!.Data);

        var managerToken = await LoginAndGetTokenAsync(managerClient, managerEmail, password);
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var fetchResponse = await managerClient.GetAsync("/api/leaveRequests");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);

        var fetchPayload = await fetchResponse.Content.ReadFromJsonAsync<ApiResponse<List<LeaveRequestApiModel>>>(JsonOptions);
        Assert.NotNull(fetchPayload);
        Assert.NotNull(fetchPayload!.Data);

        var fetchedLeave = fetchPayload.Data!.Single(x => x.Id == applyPayload.Data!.Id);
        var rowVersion = Convert.FromBase64String(fetchedLeave.RowVersion);

        var rejectResponse = await managerClient.PostAsJsonAsync("/api/leaveRequests/reject", new RejectLeaveRequest
        {
            LeaveRequestId = fetchedLeave.Id,
            RowVersion = rowVersion
        });

        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        var staleRejectResponse = await managerClient.PostAsJsonAsync("/api/leaveRequests/reject", new RejectLeaveRequest
        {
            LeaveRequestId = fetchedLeave.Id,
            RowVersion = rowVersion
        });

        Assert.Equal(HttpStatusCode.Conflict, staleRejectResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var leave = await db.LeaveRequests.SingleAsync(x => x.Id == fetchedLeave.Id);
            Assert.Equal(LeaveStatus.Rejected, leave.Status);
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
            var managerUser = await TestDataSeeder.SeedUserWithPermissionsAsync(
                db,
                managerEmail,
                passwordHash,
                [Permissions.Leave.Approve, Permissions.Leave.Reject, Permissions.Leave.View]);

            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Leave Concurrency Org {Guid.NewGuid():N}");
            var manager = TestDataSeeder.SeedEmployee(db, managerUser.Id, orgUnit.Id, null, managerEmail);
            var employee = TestDataSeeder.SeedEmployee(db, employeeUser.Id, orgUnit.Id, manager.Id, employeeEmail);
            var leaveType = TestDataSeeder.SeedLeaveType(db, $"Leave Concurrency Type {Guid.NewGuid():N}", isPaid: false, weight: 1m);

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
    private sealed record LeaveRequestApiModel(Guid Id, string Status, string RowVersion);
}
