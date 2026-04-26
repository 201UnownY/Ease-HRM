using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.IntegrationTests.TestInfrastructure;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveApplyValidationTests
{
    [Fact]
    public async Task ApplyLeave_Should_Fail_When_StartDate_In_Past()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var db = SqliteTestDb.CreateContext(connection);

        var user = TestDataSeeder.SeedUser(db);
        var managerUser = TestDataSeeder.SeedUser(db);
        var org = TestDataSeeder.SeedOrgUnit(db);

        var manager = TestDataSeeder.SeedEmployee(db, managerUser.Id, org.Id);
        var employee = TestDataSeeder.SeedEmployee(db, user.Id, org.Id, manager.Id);

        var leaveType = TestDataSeeder.SeedLeaveType(db);
        var year = DateTime.UtcNow.Year;
        _ = TestDataSeeder.SeedLeaveBalance(db, employee.Id, leaveType.Id, year, 10, 0);

        await db.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);
        var service = TestServiceFactory.CreateLeaveService(actionDb, new TestCurrentUserService(user.Id, user.Email));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ApplyLeaveAsync(new ApplyLeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                StartDate = DateTime.UtcNow.Date.AddDays(-1),
                EndDate = DateTime.UtcNow.Date.AddDays(1),
                Reason = "Past start date"
            }));
    }
}
