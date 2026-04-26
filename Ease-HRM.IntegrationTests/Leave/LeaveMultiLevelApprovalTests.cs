using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Domain.Enums;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveMultiLevelApprovalTests
{
    [Fact]
    public async Task ApproveLeave_Should_Escalate_Then_FinalApprove_In_MultiLevelHierarchy()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-multilevel@easehrm.test");
        var manager1User = TestDataSeeder.SeedUser(setupDb, "manager1-multilevel@easehrm.test");
        var manager2User = TestDataSeeder.SeedUser(setupDb, "manager2-multilevel@easehrm.test");
        var org = TestDataSeeder.SeedOrgUnit(setupDb, "MultiLevel Org");

        var manager2 = TestDataSeeder.SeedEmployee(setupDb, manager2User.Id, org.Id, null, "manager2.employee@easehrm.test");
        var manager1 = TestDataSeeder.SeedEmployee(setupDb, manager1User.Id, org.Id, manager2.Id, "manager1.employee@easehrm.test");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, org.Id, manager1.Id, "employee.multilevel@easehrm.test");

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, "MultiLevel Leave", isPaid: false);
        var year = DateTime.UtcNow.Year;

        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, 10, 0);

        var leave = TestDataSeeder.SeedLeaveRequest(
            setupDb,
            employee.Id,
            leaveType.Id,
            DateTime.UtcNow.Date.AddDays(10),
            DateTime.UtcNow.Date.AddDays(12),
            manager1.Id);

        await setupDb.SaveChangesAsync();

        await using var manager1Db = SqliteTestDb.CreateContext(connection);
        var manager1Service = TestServiceFactory.CreateLeaveService(
            manager1Db,
            new TestCurrentUserService(manager1User.Id, manager1User.Email));

        await manager1Service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leave.Id });

        await using (var assertAfterFirstDb = SqliteTestDb.CreateContext(connection))
        {
            var afterFirst = await assertAfterFirstDb.LeaveRequests.FirstAsync(x => x.Id == leave.Id);
            Assert.Equal(LeaveStatus.Pending, afterFirst.Status);
            Assert.Equal(manager2.Id, afterFirst.CurrentApproverId);
        }

        await using var manager2Db = SqliteTestDb.CreateContext(connection);
        var manager2Service = TestServiceFactory.CreateLeaveService(
            manager2Db,
            new TestCurrentUserService(manager2User.Id, manager2User.Email));

        await manager2Service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leave.Id });

        await using var finalAssertDb = SqliteTestDb.CreateContext(connection);
        var final = await finalAssertDb.LeaveRequests.FirstAsync(x => x.Id == leave.Id);
        Assert.Equal(LeaveStatus.Approved, final.Status);
        Assert.Equal(manager2.Id, final.ApprovedBy);

        var balance = await finalAssertDb.LeaveBalances.SingleAsync(x => x.EmployeeId == employee.Id && x.LeaveTypeId == leaveType.Id && x.Year == year);
        Assert.Equal(3m, balance.Used);
    }
}
