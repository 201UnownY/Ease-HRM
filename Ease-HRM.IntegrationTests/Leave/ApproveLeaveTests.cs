using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class ApproveLeaveTests
{
    [Fact]
    public async Task ApproveLeave_Should_UpdateBalance_And_CreateAuditLog()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee@easehrm.test");
        var managerUser = TestDataSeeder.SeedUser(setupDb, "manager@easehrm.test");
        var orgUnit = TestDataSeeder.SeedOrgUnit(setupDb, "Engineering");

        var manager = TestDataSeeder.SeedEmployee(setupDb, managerUser.Id, orgUnit.Id, null, "manager.employee@easehrm.test");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, orgUnit.Id, manager.Id, "employee.person@easehrm.test");

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, "Annual Leave", isPaid: false, weight: 1m);
        var year = 2026;
        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, allocated: 10, used: 0);

        var leaveRequest = TestDataSeeder.SeedLeaveRequest(
            setupDb,
            employee.Id,
            leaveType.Id,
            new DateTime(year, 3, 10),
            new DateTime(year, 3, 12),
            manager.Id);

        await setupDb.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);
        var service = TestServiceFactory.CreateLeaveService(actionDb, new TestCurrentUserService(managerUser.Id, managerUser.Email, []));

        await service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leaveRequest.Id });

        await using var assertDb = SqliteTestDb.CreateContext(connection);
        var updatedRequest = await assertDb.LeaveRequests.FirstAsync(x => x.Id == leaveRequest.Id);
        Assert.Equal("Approved", updatedRequest.Status.ToString());

        var updatedBalance = await assertDb.LeaveBalances.FirstAsync(x => x.Id != Guid.Empty && x.EmployeeId == employee.Id && x.LeaveTypeId == leaveType.Id && x.Year == year);
        Assert.Equal(3m, updatedBalance.Used);

        var audit = await assertDb.AuditLogs.FirstOrDefaultAsync(x => x.EntityName == AuditEntities.LeaveRequest && x.EntityId == leaveRequest.Id && x.Action == AuditActions.Approve);
        Assert.NotNull(audit);
    }
}
