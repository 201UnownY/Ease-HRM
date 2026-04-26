using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Services;
using Ease_HRM.Infrastructure.Data;
using Ease_HRM.Infrastructure.Repositories;
using Ease_HRM.Infrastructure.Services;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Payroll;

public class PayrollConcurrencyTests
{
    [Fact]
    public async Task GeneratePayroll_Should_CreateOnce_When_ConcurrentRequestsOccur()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-payroll-race@easehrm.test");
        var payrollUser = TestDataSeeder.SeedUser(setupDb, "payroll-race@easehrm.test");
        var orgUnit = TestDataSeeder.SeedOrgUnit(setupDb, "Finance Race");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, orgUnit.Id, null, "employee.payroll.race@easehrm.test");

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

        await setupDb.SaveChangesAsync();

        await using var db1 = SqliteTestDb.CreateContext(connection);
        await using var db2 = SqliteTestDb.CreateContext(connection);

        var currentUser1 = new TestCurrentUserService(payrollUser.Id, payrollUser.Email);
        var currentUser2 = new TestCurrentUserService(payrollUser.Id, payrollUser.Email);

        var service1 = TestServiceFactory.CreatePayrollService(db1, currentUser1);
        var service2 = TestServiceFactory.CreatePayrollService(db2, currentUser2);

        var t1 = service1.GeneratePayrollAsync(employee.Id, year, month);
        var t2 = service2.GeneratePayrollAsync(employee.Id, year, month);

        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(results[0].Id, results[1].Id);

        await using var assertDb = SqliteTestDb.CreateContext(connection);
        var payrollRows = await assertDb.Payrolls
            .Where(x => x.EmployeeId == employee.Id && x.Year == year && x.Month == month)
            .ToListAsync();

        Assert.Single(payrollRows);
    }
}
