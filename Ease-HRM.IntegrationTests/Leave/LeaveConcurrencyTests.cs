using Ease_HRM.Application.Common.Exceptions;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Enums;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveConcurrencyTests
{
    [Fact]
    public async Task ApproveLeave_Should_AllowOnlyOne_When_ConcurrentApprovals_CompeteForBalance()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-concurrency@easehrm.test");
        var managerUser = TestDataSeeder.SeedUser(setupDb, "manager-concurrency@easehrm.test");
        var orgUnit = TestDataSeeder.SeedOrgUnit(setupDb, "Operations");

        var manager = TestDataSeeder.SeedEmployee(setupDb, managerUser.Id, orgUnit.Id, null, "manager.concurrency@easehrm.test");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, orgUnit.Id, manager.Id, "employee.concurrency@easehrm.test");

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, "Sick Leave", isPaid: false, weight: 1m);
        var year = 2026;
        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, allocated: 10, used: 0);

        var leave1 = TestDataSeeder.SeedLeaveRequest(setupDb, employee.Id, leaveType.Id, new DateTime(year, 5, 1), new DateTime(year, 5, 5), manager.Id);
        var leave2 = TestDataSeeder.SeedLeaveRequest(setupDb, employee.Id, leaveType.Id, new DateTime(year, 5, 6), new DateTime(year, 5, 11), manager.Id);

        await setupDb.SaveChangesAsync();

        await using var fetchDb = SqliteTestDb.CreateContext(connection);
        var fetchedLeave1 = await fetchDb.LeaveRequests.FirstAsync(x => x.Id == leave1.Id);
        var fetchedLeave2 = await fetchDb.LeaveRequests.FirstAsync(x => x.Id == leave2.Id);

        await using var db1 = SqliteTestDb.CreateContext(connection);
        await using var db2 = SqliteTestDb.CreateContext(connection);

        var currentUser1 = new TestCurrentUserService(managerUser.Id, managerUser.Email, []);
        var currentUser2 = new TestCurrentUserService(managerUser.Id, managerUser.Email, []);

        var service1 = TestServiceFactory.CreateLeaveService(db1, currentUser1);
        var service2 = TestServiceFactory.CreateLeaveService(db2, currentUser2);

        var task1 = AttemptApproveAsync(service1, leave1.Id, fetchedLeave1.RowVersion);
        var task2 = AttemptApproveAsync(service2, leave2.Id, fetchedLeave2.RowVersion);

        await Task.WhenAll(task1, task2);

        var results = new[] { task1.Result, task2.Result };
        Assert.Equal(1, results.Count(x => x.IsSuccess));
        Assert.Equal(1, results.Count(x => !x.IsSuccess));
        Assert.Contains(results, r =>
            r.Error is ConcurrencyException or BusinessRuleException);

        await using var assertDb = SqliteTestDb.CreateContext(connection);

        var requests = await assertDb.LeaveRequests
            .Where(x => x.Id == leave1.Id || x.Id == leave2.Id)
            .ToListAsync();

        Assert.Equal(1, requests.Count(x => x.Status == LeaveStatus.Approved));
        Assert.Equal(1, requests.Count(x => x.Status != LeaveStatus.Approved));

        var balance = await assertDb.LeaveBalances.SingleAsync(x => x.EmployeeId == employee.Id && x.LeaveTypeId == leaveType.Id && x.Year == year);
        Assert.True(balance.Used == 5m || balance.Used == 6m, $"Unexpected used value: {balance.Used}");
    }

    private static async Task<(bool IsSuccess, Exception? Error)> AttemptApproveAsync(ILeaveRequestService service, Guid leaveRequestId, byte[] rowVersion)
    {
        try
        {
            await service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leaveRequestId, RowVersion = rowVersion });
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }
}
