using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Services;
using Ease_HRM.Infrastructure.Data;
using Ease_HRM.Infrastructure.Repositories;
using Ease_HRM.Infrastructure.Services;

namespace Ease_HRM.IntegrationTests.TestInfrastructure;

internal static class TestServiceFactory
{
    public static ILeaveRequestService CreateLeaveService(AppDbContext db, ICurrentUserService user)
    {
        return new LeaveRequestService(
            new LeaveRequestRepository(db),
            user,
            new AuditLogService(new AuditLogRepository(db), user),
            new ExceptionTranslator());
    }

    public static IPayrollService CreatePayrollService(AppDbContext db, ICurrentUserService user)
    {
        return new PayrollService(
            new PayrollRepository(db),
            new WorkScheduleRepository(db),
            user,
            new AuditLogService(new AuditLogRepository(db), user),
            new ExceptionTranslator());
    }
}
