using Ease_HRM.Infrastructure.Repositories;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Payroll;

public class PayrollCalculationTests
{
    [Fact]
    public async Task GeneratePayroll_Should_Calculate_Correct_NetSalary_And_Be_Idempotent()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-payroll@easehrm.test");
        var payrollUser = TestDataSeeder.SeedUser(setupDb, "payroll-operator@easehrm.test");
        var orgUnit = TestDataSeeder.SeedOrgUnit(setupDb, "Finance");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, orgUnit.Id, null, "payroll.employee@easehrm.test");

        const int year = 2026;
        const int month = 4;

        _ = TestDataSeeder.SeedSalaryStructure(setupDb, employee.Id, baseSalary: 30000m, hra: 0m, allowances: 0m, deductions: 0m, effectiveFrom: new DateTime(year, 1, 1));
        _ = TestDataSeeder.SeedAttendancePolicy(setupDb, new DateTime(year, 1, 1), fullDayHours: 8m, halfDayHours: 4m);
        _ = TestDataSeeder.SeedWorkSchedule(setupDb, employee.Id, new DateTime(year, 1, 1));

        var unpaidLeaveType = TestDataSeeder.SeedLeaveType(setupDb, "Unpaid Leave", isPaid: false, weight: 1m);
        _ = TestDataSeeder.SeedApprovedLeave(setupDb, employee.Id, unpaidLeaveType.Id, new DateTime(year, month, 1), new DateTime(year, month, 2));

        var absentDate = new DateTime(year, month, 3);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
        {
            if (day.Day <= 2 || day.Date == absentDate.Date)
            {
                continue;
            }

            _ = TestDataSeeder.SeedAttendanceSession(setupDb, employee.Id, day, TimeSpan.FromHours(8));
        }

        await setupDb.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);
        var currentUserService = new TestCurrentUserService(payrollUser.Id, payrollUser.Email, []);
        var service = TestServiceFactory.CreatePayrollService(actionDb, currentUserService);

        var payroll = await service.GeneratePayrollAsync(employee.Id, year, month);

        Assert.NotNull(payroll);

        var workingUnits = (await new WorkScheduleRepository(actionDb).GetWorkingDateWeights(employee.Id, year, month)).Values.Sum();
        var perDay = 30000m / workingUnits;

        var expectedLeaveDeduction = Math.Round(perDay * 2m, 2, MidpointRounding.AwayFromZero);
        var expectedAttendanceDeduction = Math.Round(perDay * 1m, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedLeaveDeduction, payroll.LeaveDeduction);
        Assert.Equal(expectedAttendanceDeduction, payroll.AttendanceDeduction);
        Assert.Equal(27000m, payroll.NetSalary);

        var secondRun = await service.GeneratePayrollAsync(employee.Id, year, month);
        Assert.Equal(payroll.Id, secondRun.Id);

        await using var assertDb = SqliteTestDb.CreateContext(connection);
        var payrollRows = await assertDb.Payrolls.Where(x => x.EmployeeId == employee.Id && x.Year == year && x.Month == month).ToListAsync();
        Assert.Single(payrollRows);
    }
}
