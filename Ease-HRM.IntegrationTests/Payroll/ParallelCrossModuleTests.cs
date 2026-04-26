using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Employees;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.DTOs.Payroll;
using Ease_HRM.Domain.Enums;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Payroll;

public class ParallelCrossModuleTests
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
    public async Task LeaveApproval_And_PayrollGeneration_InParallel_RemainsConsistent()
    {
        await using var factory = new ApiWebApplicationFactory();
        var operatorClient = factory.CreateClient(ClientOptions);
        var employeeClient = factory.CreateClient(ClientOptions);
        var managerClient = factory.CreateClient(ClientOptions);

        var operatorEmail = $"parallel-payroll-operator-{Guid.NewGuid():N}@easehrm.test";
        var employeeEmail = $"parallel-payroll-employee-{Guid.NewGuid():N}@easehrm.test";
        var managerEmail = $"parallel-payroll-manager-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var leaveStart = today <= monthEnd.AddDays(-2) ? today.AddDays(1) : today;
        var leaveEnd = leaveStart.AddDays(Math.Min(2, (monthEnd - leaveStart).Days));
        var leaveDays = (decimal)(leaveEnd.Date - leaveStart.Date).TotalDays + 1m;

        var seed = await SeedCrossModuleScenarioAsync(factory, operatorEmail, employeeEmail, managerEmail, password);

        var operatorToken = await LoginAndGetTokenAsync(operatorClient, operatorEmail, password);
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        var createEmployeeResponse = await operatorClient.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest
        {
            UserId = seed.EmployeeUserId,
            FirstName = "Parallel",
            LastName = "Module",
            Email = employeeEmail,
            Phone = "8888888888",
            OrgUnitId = seed.OrgUnitId,
            ManagerId = seed.ManagerEmployeeId,
            JoinDate = today.AddDays(-30)
        });

        Assert.Equal(HttpStatusCode.OK, createEmployeeResponse.StatusCode);

        var createEmployeePayload = await createEmployeeResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>(JsonOptions);
        Assert.NotNull(createEmployeePayload);
        Assert.NotNull(createEmployeePayload!.Data);

        var employee = createEmployeePayload.Data!;

        await SeedPayrollAndLeavePrerequisitesAsync(factory, employee.Id, seed.LeaveTypeId, monthStart, leaveStart, leaveEnd);

        var employeeToken = await LoginAndGetTokenAsync(employeeClient, employeeEmail, password);
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var applyResponse = await employeeClient.PostAsJsonAsync("/api/leaveRequests/apply", new ApplyLeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = seed.LeaveTypeId,
            StartDate = leaveStart,
            EndDate = leaveEnd,
            Reason = "Parallel cross-module validation"
        });

        Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

        var applyPayload = await applyResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(applyPayload);
        Assert.NotNull(applyPayload!.Data);
        Assert.Equal("Pending", applyPayload.Data!.Status);

        var managerToken = await LoginAndGetTokenAsync(managerClient, managerEmail, password);
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        var approveTask = managerClient.PostAsJsonAsync("/api/leaveRequests/approve", new ApproveLeaveRequest
        {
            LeaveRequestId = applyPayload.Data.Id,
            RowVersion = applyPayload.Data.RowVersion
        });

        var generateTask = operatorClient.PostAsync($"/api/payroll/generate?employeeId={employee.Id}&year={today.Year}&month={today.Month}", content: null);

        await Task.WhenAll(approveTask, generateTask);

        var approveResponse = await approveTask;
        var generateResponse = await generateTask;

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

        var approvePayload = await approveResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestDto>>(JsonOptions);
        Assert.NotNull(approvePayload);
        Assert.NotNull(approvePayload!.Data);
        Assert.Equal("Approved", approvePayload.Data!.Status);

        var generatePayload = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollDto>>(JsonOptions);
        Assert.NotNull(generatePayload);
        Assert.NotNull(generatePayload!.Data);

        var payroll = generatePayload.Data!;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var expectedLeaveDeduction = Math.Round((30000m / daysInMonth) * leaveDays, 2, MidpointRounding.AwayFromZero);
        var expectedNetSalaryWithLeave = Math.Round(30000m - expectedLeaveDeduction, 2, MidpointRounding.AwayFromZero);

        var totalApiDeduction = Math.Round(payroll.LeaveDeduction + payroll.AttendanceDeduction, 2, MidpointRounding.AwayFromZero);
        Assert.Contains(Math.Round(payroll.LeaveDeduction, 2, MidpointRounding.AwayFromZero), new[] { 0m, expectedLeaveDeduction });
        Assert.Contains(Math.Round(payroll.AttendanceDeduction, 2, MidpointRounding.AwayFromZero), new[] { 0m, expectedLeaveDeduction });
        Assert.Contains(totalApiDeduction, new[] { expectedLeaveDeduction });
        Assert.Equal(expectedNetSalaryWithLeave, payroll.NetSalary);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var leaveRequest = await db.LeaveRequests.SingleAsync(x => x.Id == applyPayload.Data.Id);
            var leaveBalance = await db.LeaveBalances.SingleAsync(x =>
                x.EmployeeId == employee.Id &&
                x.LeaveTypeId == seed.LeaveTypeId &&
                x.Year == today.Year);
            var payrollRows = await db.Payrolls
                .Where(x => x.EmployeeId == employee.Id && x.Year == today.Year && x.Month == today.Month)
                .ToListAsync();

            Assert.Single(payrollRows);

            var payrollRow = payrollRows[0];

            Assert.Equal(LeaveStatus.Approved, leaveRequest.Status);
            Assert.Equal(leaveDays, leaveBalance.Used);
            var roundedLeaveDeduction = Math.Round(payrollRow.LeaveDeduction, 2, MidpointRounding.AwayFromZero);
            var roundedAttendanceDeduction = Math.Round(payrollRow.AttendanceDeduction, 2, MidpointRounding.AwayFromZero);
            var totalDbDeduction = Math.Round(roundedLeaveDeduction + roundedAttendanceDeduction, 2, MidpointRounding.AwayFromZero);

            Assert.Contains(roundedLeaveDeduction, new[] { 0m, expectedLeaveDeduction });
            Assert.Contains(roundedAttendanceDeduction, new[] { 0m, expectedLeaveDeduction });
            Assert.Equal(expectedLeaveDeduction, totalDbDeduction);
            Assert.Equal(expectedNetSalaryWithLeave, Math.Round(payrollRow.NetSalary, 2, MidpointRounding.AwayFromZero));
        });
    }

    private static async Task<CrossModuleSeedData> SeedCrossModuleScenarioAsync(
        ApiWebApplicationFactory factory,
        string operatorEmail,
        string employeeEmail,
        string managerEmail,
        string password)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        var seed = new CrossModuleSeedData(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);

        await factory.ExecuteDbContextAsync(async db =>
        {
            _ = await TestDataSeeder.SeedUserWithPermissionsAsync(
                db,
                operatorEmail,
                passwordHash,
                [Permissions.Employee.Create, Permissions.Payroll.Generate]);

            var employeeUser = await TestDataSeeder.SeedUserWithPermissionAsync(
                db,
                employeeEmail,
                passwordHash,
                Permissions.Leave.Apply);

            var managerUser = await TestDataSeeder.SeedUserWithPermissionAsync(
                db,
                managerEmail,
                passwordHash,
                Permissions.Leave.Approve);

            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Parallel Cross Module Org {Guid.NewGuid():N}");
            var managerEmployee = TestDataSeeder.SeedEmployee(db, managerUser.Id, orgUnit.Id, null, managerEmail);
            var leaveType = TestDataSeeder.SeedLeaveType(db, $"Parallel Cross Module Leave {Guid.NewGuid():N}", isPaid: false, weight: 1m);

            await db.SaveChangesAsync();

            seed = new CrossModuleSeedData(employeeUser.Id, orgUnit.Id, managerEmployee.Id, leaveType.Id);
        });

        return seed;
    }

    private static async Task SeedPayrollAndLeavePrerequisitesAsync(
        ApiWebApplicationFactory factory,
        Guid employeeId,
        Guid leaveTypeId,
        DateTime monthStart,
        DateTime leaveStart,
        DateTime leaveEnd)
    {
        await factory.ExecuteDbContextAsync(async db =>
        {
            _ = TestDataSeeder.SeedSalaryStructure(db, employeeId, baseSalary: 30000m, hra: 0m, allowances: 0m, deductions: 0m, effectiveFrom: monthStart);
            _ = TestDataSeeder.SeedAttendancePolicy(db, monthStart, fullDayHours: 8m, halfDayHours: 4m);
            _ = TestDataSeeder.SeedWorkSchedule(db, employeeId, monthStart);
            _ = TestDataSeeder.SeedLeaveBalance(db, employeeId, leaveTypeId, monthStart.Year, allocated: 10m, used: 0m);

            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
            {
                if (day >= leaveStart && day <= leaveEnd)
                {
                    continue;
                }

                _ = TestDataSeeder.SeedAttendanceSession(db, employeeId, day, TimeSpan.FromHours(8));
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
    private sealed record CrossModuleSeedData(Guid EmployeeUserId, Guid OrgUnitId, Guid ManagerEmployeeId, Guid LeaveTypeId);
}
