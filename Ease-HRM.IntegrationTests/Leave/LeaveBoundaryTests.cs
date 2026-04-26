using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveBoundaryTests
{
    [Fact]
    public async Task Leave_Across_Month_Boundary_Should_Calculate_Correctly()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var user = TestDataSeeder.SeedUser(setupDb);
        var managerUser = TestDataSeeder.SeedUser(setupDb);
        var org = TestDataSeeder.SeedOrgUnit(setupDb);

        var manager = TestDataSeeder.SeedEmployee(setupDb, managerUser.Id, org.Id);
        var employee = TestDataSeeder.SeedEmployee(setupDb, user.Id, org.Id, manager.Id);

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, isPaid: false);
        var year = 2026;

        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, 10, 0);

        var leave = TestDataSeeder.SeedLeaveRequest(
            setupDb,
            employee.Id,
            leaveType.Id,
            new DateTime(year, 1, 30),
            new DateTime(year, 2, 2),
            manager.Id);

        await setupDb.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);

        var service = TestServiceFactory.CreateLeaveService(
            actionDb,
            new TestCurrentUserService(managerUser.Id, managerUser.Email));

        await service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leave.Id });

        await using var assertDb = SqliteTestDb.CreateContext(connection);

        var balance = await assertDb.LeaveBalances
            .SingleAsync(x => x.EmployeeId == employee.Id && x.Year == year);

        Assert.Equal(4m, balance.Used);
    }
}
