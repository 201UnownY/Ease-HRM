using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.IntegrationTests.TestInfrastructure;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveOverlappingTests
{
    [Fact]
    public async Task ApplyLeave_Should_Fail_When_Overlapping_Leave_Exists()
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

        _ = TestDataSeeder.SeedLeaveRequest(
            db,
            employee.Id,
            leaveType.Id,
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(7),
            manager.Id);

        await db.SaveChangesAsync();

        await using var actionDb = SqliteTestDb.CreateContext(connection);

        var service = TestServiceFactory.CreateLeaveService(
            actionDb,
            new TestCurrentUserService(user.Id, user.Email));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ApplyLeaveAsync(new ApplyLeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                StartDate = DateTime.UtcNow.Date.AddDays(6),
                EndDate = DateTime.UtcNow.Date.AddDays(8),
                Reason = "Overlap test"
            }));

        Assert.IsType<BusinessRuleException>(ex);
    }
}
