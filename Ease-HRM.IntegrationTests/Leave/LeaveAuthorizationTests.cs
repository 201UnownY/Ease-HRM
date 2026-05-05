using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Services;
using Ease_HRM.Infrastructure.Data;
using Ease_HRM.Infrastructure.Repositories;
using Ease_HRM.Infrastructure.Services;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveAuthorizationTests
{
    [Fact]
    public async Task NonManager_Should_Not_Be_Able_To_Approve()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-auth@easehrm.test");
        var managerUser = TestDataSeeder.SeedUser(setupDb, "manager-auth@easehrm.test");
        var otherUser = TestDataSeeder.SeedUser(setupDb, "other-auth@easehrm.test");

        var org = TestDataSeeder.SeedOrgUnit(setupDb, "Authorization Org");

        var manager = TestDataSeeder.SeedEmployee(setupDb, managerUser.Id, org.Id, null, "manager.auth@easehrm.test");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, org.Id, manager.Id, "employee.auth@easehrm.test");
        var notManager = TestDataSeeder.SeedEmployee(setupDb, otherUser.Id, org.Id, null, "notmanager.auth@easehrm.test");

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, "Authorization Leave");
        var year = 2026;

        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, 10, 0);

        var leave = TestDataSeeder.SeedLeaveRequest(
            setupDb,
            employee.Id,
            leaveType.Id,
            new DateTime(year, 7, 1),
            new DateTime(year, 7, 2),
            manager.Id);

        await setupDb.SaveChangesAsync();

        await using var fetchDb = SqliteTestDb.CreateContext(connection);
        var fetchedLeave = await fetchDb.LeaveRequests.FirstAsync(x => x.Id == leave.Id);

        await using var actionDb = SqliteTestDb.CreateContext(connection);

        var service = TestServiceFactory.CreateLeaveService(actionDb, new TestCurrentUserService(notManager.UserId, notManager.Email));

        var ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leave.Id, RowVersion = fetchedLeave.RowVersion }));

        Assert.IsType<AuthorizationException>(ex);
    }
}
