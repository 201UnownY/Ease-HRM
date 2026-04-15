using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ease_HRM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IOrgUnitService, OrgUnitService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();

        return services;
    }
}
