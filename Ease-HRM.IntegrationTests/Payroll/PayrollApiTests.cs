using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ease_HRM.Api.Models;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Payroll;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.IntegrationTests.Payroll;

public class PayrollApiTests
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
    public async Task GenerateAndProcessPayroll_WithStaleSecondProcess_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var operatorEmail = $"payroll-api-operator-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedPayrollScenarioAsync(factory, operatorEmail, password);

        var token = await LoginAndGetTokenAsync(client, operatorEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var generateResponse = await client.PostAsync($"/api/payroll/generate?employeeId={seed.EmployeeId}&year={seed.Year}&month={seed.Month}", content: null);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

        var generatePayload = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollDto>>(JsonOptions);
        Assert.NotNull(generatePayload);
        Assert.True(generatePayload!.Success);
        Assert.NotNull(generatePayload.Data);

        var processRequest = new ProcessPayrollRequest
        {
            PayrollId = generatePayload.Data!.Id,
            RowVersion = generatePayload.Data.RowVersion
        };

        var processResponse = await client.PostAsJsonAsync("/api/payroll/process", processRequest);
        Assert.Equal(HttpStatusCode.OK, processResponse.StatusCode);

        var processPayload = await processResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollDto>>(JsonOptions);
        Assert.NotNull(processPayload);
        Assert.True(processPayload!.Success);
        Assert.NotNull(processPayload.Data);

        var staleProcessResponse = await client.PostAsJsonAsync("/api/payroll/process", processRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleProcessResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var payroll = await db.Payrolls.SingleAsync(x => x.Id == generatePayload.Data.Id);
            Assert.Equal(seed.EmployeeId, payroll.EmployeeId);
            Assert.Equal(seed.Year, payroll.Year);
            Assert.Equal(seed.Month, payroll.Month);
        });
    }

    [Fact]
    public async Task AdjustPayroll_WithStaleRowVersion_Returns409_AndPreservesLatestState()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(ClientOptions);

        var operatorEmail = $"payroll-api-adjust-{Guid.NewGuid():N}@easehrm.test";
        const string password = "ValidP@ssw0rd";

        var seed = await SeedPayrollScenarioAsync(factory, operatorEmail, password);

        var token = await LoginAndGetTokenAsync(client, operatorEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var generateResponse = await client.PostAsync($"/api/payroll/generate?employeeId={seed.EmployeeId}&year={seed.Year}&month={seed.Month}", content: null);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);

        var generatePayload = await generateResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollDto>>(JsonOptions);
        Assert.NotNull(generatePayload);
        Assert.NotNull(generatePayload!.Data);

        var fetchResponse = await client.GetAsync($"/api/payroll/{seed.EmployeeId}");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);

        var fetchPayload = await fetchResponse.Content.ReadFromJsonAsync<ApiResponse<List<PayrollDto>>>(JsonOptions);
        Assert.NotNull(fetchPayload);
        Assert.NotNull(fetchPayload!.Data);

        var fetchedPayroll = fetchPayload.Data!.Single(x => x.Id == generatePayload.Data.Id);

        var adjustRequest = new AdjustPayrollRequest
        {
            PayrollId = fetchedPayroll.Id,
            LeaveDeduction = fetchedPayroll.LeaveDeduction + 100m,
            AttendanceDeduction = fetchedPayroll.AttendanceDeduction + 50m,
            RowVersion = fetchedPayroll.RowVersion
        };

        var adjustResponse = await client.PutAsJsonAsync("/api/payroll/adjust", adjustRequest);
        Assert.Equal(HttpStatusCode.OK, adjustResponse.StatusCode);

        var adjustPayload = await adjustResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollDto>>(JsonOptions);
        Assert.NotNull(adjustPayload);
        Assert.NotNull(adjustPayload!.Data);

        var staleAdjustRequest = new AdjustPayrollRequest
        {
            PayrollId = fetchedPayroll.Id,
            LeaveDeduction = adjustRequest.LeaveDeduction + 25m,
            AttendanceDeduction = adjustRequest.AttendanceDeduction + 25m,
            RowVersion = fetchedPayroll.RowVersion
        };

        var staleAdjustConflictResponse = await client.PutAsJsonAsync("/api/payroll/adjust", staleAdjustRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleAdjustConflictResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async db =>
        {
            var payroll = await db.Payrolls.SingleAsync(x => x.Id == generatePayload.Data.Id);
            Assert.Equal(adjustRequest.LeaveDeduction, payroll.LeaveDeduction);
            Assert.Equal(adjustRequest.AttendanceDeduction, payroll.AttendanceDeduction);
        });
    }

    private static async Task<PayrollApiSeedData> SeedPayrollScenarioAsync(
        ApiWebApplicationFactory factory,
        string operatorEmail,
        string password)
    {
        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(password);

        var seed = new PayrollApiSeedData(Guid.Empty, 0, 0);
        var payrollPeriod = DateTime.UtcNow.AddMonths(-1);
        var year = payrollPeriod.Year;
        var month = payrollPeriod.Month;

        await factory.ExecuteDbContextAsync(async db =>
        {
            var employeeUser = TestDataSeeder.SeedUser(db, $"payroll-api-employee-{Guid.NewGuid():N}@easehrm.test");
            _ = await TestDataSeeder.SeedUserWithPermissionsAsync(
                db,
                operatorEmail,
                passwordHash,
                [Permissions.Payroll.Generate, Permissions.Payroll.ManageSalaryStructure, Permissions.Payroll.View]);

            var orgUnit = TestDataSeeder.SeedOrgUnit(db, $"Payroll Api Org {Guid.NewGuid():N}");
            var employee = TestDataSeeder.SeedEmployee(db, employeeUser.Id, orgUnit.Id, null, employeeUser.Email);

            _ = TestDataSeeder.SeedSalaryStructure(db, employee.Id, baseSalary: 30000m, hra: 0m, allowances: 0m, deductions: 0m, effectiveFrom: new DateTime(year, 1, 1));
            _ = TestDataSeeder.SeedAttendancePolicy(db, new DateTime(year, 1, 1), fullDayHours: 8m, halfDayHours: 4m);
            _ = TestDataSeeder.SeedWorkSchedule(db, employee.Id, new DateTime(year, 1, 1));

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
            {
                _ = TestDataSeeder.SeedAttendanceSession(db, employee.Id, day, TimeSpan.FromHours(8));
            }

            await db.SaveChangesAsync();

            seed = new PayrollApiSeedData(employee.Id, year, month);
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
    private sealed record PayrollApiSeedData(Guid EmployeeId, int Year, int Month);
}
