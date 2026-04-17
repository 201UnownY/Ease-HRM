using Ease_HRM.Domain.Entities;
using Ease_HRM.Application.Constants;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Api.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var permissions = new[]
        {
            Permissions.User.Create,
            Permissions.User.View,
            Permissions.Role.Create,
            Permissions.Role.View,
            Permissions.Permission.Create,
            Permissions.Permission.View,
            Permissions.UserRole.Assign,
            Permissions.UserRole.View,
            Permissions.RolePermission.Assign,
            Permissions.RolePermission.View,
            Permissions.OrgUnit.Create,
            Permissions.OrgUnit.View,
            Permissions.Employee.Create,
            Permissions.Employee.View,
            Permissions.Employee.Update,
            Permissions.LeaveType.Create,
            Permissions.LeaveType.View,
            Permissions.Leave.Apply,
            Permissions.Leave.Approve,
            Permissions.Leave.Reject,
            Permissions.Leave.View,
            Permissions.Attendance.CheckIn,
            Permissions.Attendance.CheckOut,
            Permissions.Attendance.ManagePolicy,
            Permissions.Attendance.View,
            Permissions.Payroll.Generate,
            Permissions.Payroll.View,
            Permissions.Payroll.ManageSalaryStructure
        };

        var now = DateTime.UtcNow;

        foreach (var name in permissions)
        {
            if (!await context.Permissions.AnyAsync(x => x.Name == name))
            {
                context.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedAt = now,
                    UpdatedBy = Guid.Empty
                });
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // ignore duplicate insert race
        }
    }
}