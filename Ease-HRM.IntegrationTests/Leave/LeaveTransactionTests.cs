using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Services;
using Ease_HRM.Domain.Enums;
using Ease_HRM.Infrastructure.Repositories;
using Ease_HRM.Infrastructure.Services;
using Ease_HRM.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.Leave;

public class LeaveTransactionTests
{
    [Fact]
    public async Task ApproveLeave_Should_Rollback_When_AuditFails()
    {
        await using var connection = SqliteTestDb.CreateOpenConnection();
        await using var setupDb = SqliteTestDb.CreateContext(connection);

        var employeeUser = TestDataSeeder.SeedUser(setupDb, "employee-rollback@easehrm.test");
        var managerUser = TestDataSeeder.SeedUser(setupDb, "manager-rollback@easehrm.test");
        var org = TestDataSeeder.SeedOrgUnit(setupDb, "Rollback Org");

        var manager = TestDataSeeder.SeedEmployee(setupDb, managerUser.Id, org.Id, null, "manager.rollback@easehrm.test");
        var employee = TestDataSeeder.SeedEmployee(setupDb, employeeUser.Id, org.Id, manager.Id, "employee.rollback@easehrm.test");

        var leaveType = TestDataSeeder.SeedLeaveType(setupDb, "Rollback Leave", isPaid: false);
        var year = 2026;

        _ = TestDataSeeder.SeedLeaveBalance(setupDb, employee.Id, leaveType.Id, year, 10, 0);

        var leave = TestDataSeeder.SeedLeaveRequest(
            setupDb,
            employee.Id,
            leaveType.Id,
            new DateTime(year, 6, 1),
            new DateTime(year, 6, 3),
            manager.Id);

        await setupDb.SaveChangesAsync();

        await using var fetchDb = SqliteTestDb.CreateContext(connection);
        var fetchedLeave = await fetchDb.LeaveRequests.FirstAsync(x => x.Id == leave.Id);

        await using var actionDb = SqliteTestDb.CreateContext(connection);

        var service = new LeaveRequestService(
            new LeaveRequestRepository(actionDb),
            new TestCurrentUserService(managerUser.Id, managerUser.Email),
            new FailingAuditLogService(),
            new ExceptionTranslator());

        await Assert.ThrowsAsync<Exception>(() =>
            service.ApproveLeaveAsync(new ApproveLeaveRequest { LeaveRequestId = leave.Id, RowVersion = fetchedLeave.RowVersion }));

        await using var assertDb = SqliteTestDb.CreateContext(connection);

        var request = await assertDb.LeaveRequests.FirstAsync(x => x.Id == leave.Id);
        var balance = await assertDb.LeaveBalances.FirstAsync(x => x.EmployeeId == employee.Id && x.LeaveTypeId == leaveType.Id && x.Year == year);
        var auditLogs = await assertDb.AuditLogs.Where(x => x.EntityId == leave.Id).ToListAsync();

        Assert.Equal(LeaveStatus.Pending, request.Status);
        Assert.Equal(0m, balance.Used);
        Assert.Empty(auditLogs);
    }

    private sealed class FailingAuditLogService : IAuditLogService
    {
        public Task LogAsync(string action, string entityName, Guid? entityId = null, string? details = null, CancellationToken cancellationToken = default)
            => throw new Exception("Audit failure");
    }
}
