using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.IntegrationTests.TestInfrastructure;

namespace Ease_HRM.IntegrationTests.Payroll;

public class PayrollWorkScheduleValidationTests
{
    [Fact]
    public async Task GeneratePayroll_Should_Fail_When_WorkSchedule_NotConfigured()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-noschedule@easehrm.test");
        var payrollUser = TestDataSeeder.SeedUser(setupDb, "payroll-noschedule@easehrm.test");
        var org = TestDataSeeder.SeedOrgUnit(setupDb, "NoSchedule Org");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, org.Id, null, "employee.noschedule@easehrm.test");

        var payrollPeriod = DateTime.UtcNow.AddMonths(-1);
        var year = payrollPeriod.Year;
        var month = payrollPeriod.Month;

        _ = TestDataSeeder.SeedSalaryStructure(setupDb, employee.Id, baseSalary: 30000m, effectiveFrom: new DateTime(year, 1, 1));
        _ = TestDataSeeder.SeedAttendancePolicy(setupDb, new DateTime(year, 1, 1));
        _ = TestDataSeeder.SeedWorkSchedule(setupDb, employee.Id, new DateTime(year, 1, 1));

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
        {
            _ = TestDataSeeder.SeedAttendanceSession(setupDb, employee.Id, day, TimeSpan.FromHours(8));
        }

        var overridingZeroWeightSchedule = TestDataSeeder.SeedWorkSchedule(setupDb, employee.Id, monthStart);
        overridingZeroWeightSchedule.MondayWeight = 0m;
        overridingZeroWeightSchedule.TuesdayWeight = 0m;
        overridingZeroWeightSchedule.WednesdayWeight = 0m;
        overridingZeroWeightSchedule.ThursdayWeight = 0m;
        overridingZeroWeightSchedule.FridayWeight = 0m;
        overridingZeroWeightSchedule.SaturdayWeight = 0m;
        overridingZeroWeightSchedule.SundayWeight = 0m;

        await setupDb.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);
        var service = TestServiceFactory.CreatePayrollService(actionDb, new TestCurrentUserService(payrollUser.Id, payrollUser.Email));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GeneratePayrollAsync(employee.Id, year, month));

        Assert.Equal("Work schedule not configured for the selected period.", ex.Message);
    }
}
