using Ease_HRM.IntegrationTests.TestInfrastructure;

namespace Ease_HRM.IntegrationTests.Payroll;

public class PayrollHalfDayTests
{
    [Fact]
    public async Task GeneratePayroll_Should_Apply_HalfDay_Attendance_Deduction()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-halfday@easehrm.test");
        var payrollUser = TestDataSeeder.SeedUser(setupDb, "payroll-halfday@easehrm.test");
        var org = TestDataSeeder.SeedOrgUnit(setupDb, "HalfDay Org");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, org.Id, null, "employee.halfday@easehrm.test");

        var payrollPeriod = DateTime.UtcNow.AddMonths(-1);
        var year = payrollPeriod.Year;
        var month = payrollPeriod.Month;

        _ = TestDataSeeder.SeedSalaryStructure(setupDb, employee.Id, baseSalary: 30000m, effectiveFrom: new DateTime(year, 1, 1));
        _ = TestDataSeeder.SeedAttendancePolicy(setupDb, new DateTime(year, 1, 1), fullDayHours: 8m, halfDayHours: 4m);
        _ = TestDataSeeder.SeedWorkSchedule(setupDb, employee.Id, new DateTime(year, 1, 1));

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var halfDayDate = monthStart;
        _ = TestDataSeeder.SeedAttendanceSession(setupDb, employee.Id, halfDayDate, TimeSpan.FromHours(4));

        for (var day = monthStart.AddDays(1); day <= monthEnd; day = day.AddDays(1))
        {
            _ = TestDataSeeder.SeedAttendanceSession(setupDb, employee.Id, day, TimeSpan.FromHours(8));
        }

        await setupDb.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);
        var service = TestServiceFactory.CreatePayrollService(actionDb, new TestCurrentUserService(payrollUser.Id, payrollUser.Email));

        var payroll = await service.GeneratePayrollAsync(employee.Id, year, month);

        var perDay = Math.Round(30000m / DateTime.DaysInMonth(year, month), 4);
        var expectedAttendanceDeduction = Math.Round(perDay * 0.5m, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedAttendanceDeduction, payroll.AttendanceDeduction);
    }
}
