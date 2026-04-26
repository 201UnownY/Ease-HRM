using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.IntegrationTests.TestInfrastructure;

internal static class TestDataSeeder
{
    public static User SeedUser(AppDbContext db, string? email = null, string? passwordHash = null, bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"user-{Guid.NewGuid():N}@easehrm.test",
            PasswordHash = passwordHash ?? "hash",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        db.Users.Add(user);
        return user;
    }

    public static Role SeedRole(AppDbContext db, string? name = null, bool isActive = true)
    {
        var now = DateTime.UtcNow;
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"role-{Guid.NewGuid():N}",
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        db.Roles.Add(role);
        return role;
    }

    public static Permission SeedPermission(AppDbContext db, string name)
    {
        var now = DateTime.UtcNow;
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        db.Permissions.Add(permission);
        return permission;
    }

    public static async Task<Permission> EnsurePermissionAsync(AppDbContext db, string permissionName, CancellationToken cancellationToken = default)
    {
        var existing = await db.Permissions
            .SingleOrDefaultAsync(x => x.Name == permissionName, cancellationToken);

        return existing ?? SeedPermission(db, permissionName);
    }

    public static async Task<User> SeedUserWithPermissionAsync(
        AppDbContext db,
        string email,
        string passwordHash,
        string permissionName,
        CancellationToken cancellationToken = default)
    {
        return await SeedUserWithPermissionsAsync(db, email, passwordHash, [permissionName], cancellationToken);
    }

    public static async Task<User> SeedUserWithPermissionsAsync(
        AppDbContext db,
        string email,
        string passwordHash,
        IEnumerable<string> permissionNames,
        CancellationToken cancellationToken = default)
    {
        var user = SeedUser(db, email, passwordHash, isActive: true);
        var role = SeedRole(db);
        SeedUserRole(db, user.Id, role.Id);

        foreach (var permissionName in permissionNames.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var permission = await EnsurePermissionAsync(db, permissionName, cancellationToken);
            SeedRolePermission(db, role.Id, permission.Id);
        }

        return user;
    }

    public static UserRole SeedUserRole(AppDbContext db, Guid userId, Guid roleId)
    {
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId
        };

        db.UserRoles.Add(userRole);
        return userRole;
    }

    public static RolePermission SeedRolePermission(AppDbContext db, Guid roleId, Guid permissionId)
    {
        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        db.RolePermissions.Add(rolePermission);
        return rolePermission;
    }

    public static OrgUnit SeedOrgUnit(AppDbContext db, string? name = null)
    {
        var orgUnit = new OrgUnit
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Org-{Guid.NewGuid():N}",
            Level = 1,
            IsActive = true
        };

        db.OrgUnits.Add(orgUnit);
        return orgUnit;
    }

    public static Ease_HRM.Domain.Entities.Employee SeedEmployee(AppDbContext db, Guid userId, Guid orgUnitId, Guid? managerId = null, string? email = null)
    {
        var employee = new Ease_HRM.Domain.Entities.Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Test",
            LastName = "Employee",
            Email = email ?? $"employee-{Guid.NewGuid():N}@easehrm.test",
            Phone = "0000000000",
            OrgUnitId = orgUnitId,
            ManagerId = managerId,
            JoinDate = DateTime.UtcNow.Date.AddYears(-1),
            IsActive = true
        };

        db.Employees.Add(employee);
        return employee;
    }

    public static LeaveType SeedLeaveType(AppDbContext db, string? name = null, bool isPaid = false, decimal weight = 1m)
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"LeaveType-{Guid.NewGuid():N}",
            DefaultDays = 12,
            Weight = weight,
            IsPaid = isPaid
        };

        db.LeaveTypes.Add(leaveType);
        return leaveType;
    }

    public static LeaveBalance SeedLeaveBalance(AppDbContext db, Guid employeeId, Guid leaveTypeId, int year, decimal allocated, decimal used, decimal carryForward = 0)
    {
        var balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            Allocated = allocated,
            Used = used,
            CarryForward = carryForward
        };

        db.LeaveBalances.Add(balance);
        return balance;
    }

    public static LeaveRequest SeedLeaveRequest(AppDbContext db, Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, Guid? currentApproverId)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = LeaveStatus.Pending,
            Reason = "Integration test",
            AppliedOn = DateTime.UtcNow,
            CurrentApproverId = currentApproverId,
            ApprovedBy = null,
            ApprovedOn = null,
            IsDeleted = false
        };

        db.LeaveRequests.Add(leaveRequest);
        return leaveRequest;
    }

    public static SalaryStructure SeedSalaryStructure(AppDbContext db, Guid employeeId, decimal baseSalary, decimal hra = 0, decimal allowances = 0, decimal deductions = 0, DateTime? effectiveFrom = null)
    {
        var now = DateTime.UtcNow;
        var salary = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            BaseSalary = baseSalary,
            HRA = hra,
            Allowances = allowances,
            Deductions = deductions,
            EffectiveFrom = (effectiveFrom ?? new DateTime(2026, 1, 1)).Date,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            ChangeReason = "Seed",
            IsDeleted = false
        };

        db.SalaryStructures.Add(salary);
        return salary;
    }

    public static AttendancePolicy SeedAttendancePolicy(AppDbContext db, DateTime effectiveFrom, decimal fullDayHours = 8m, decimal halfDayHours = 4m)
    {
        var now = DateTime.UtcNow;
        var policy = new AttendancePolicy
        {
            Id = Guid.NewGuid(),
            FullDayHours = fullDayHours,
            HalfDayHours = halfDayHours,
            EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            ChangeReason = "Seed"
        };

        db.AttendancePolicies.Add(policy);
        return policy;
    }

    public static WorkSchedule SeedWorkSchedule(AppDbContext db, Guid employeeId, DateTime effectiveFrom)
    {
        var now = DateTime.UtcNow;
        var schedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrgUnitId = null,
            EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = null,
            MondayWeight = 1m,
            TuesdayWeight = 1m,
            WednesdayWeight = 1m,
            ThursdayWeight = 1m,
            FridayWeight = 1m,
            SaturdayWeight = 1m,
            SundayWeight = 1m,
            ShiftCode = "GEN",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            ChangeReason = "Seed"
        };

        db.WorkSchedules.Add(schedule);
        return schedule;
    }

    public static LeaveRequest SeedApprovedLeave(AppDbContext db, Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = LeaveStatus.Approved,
            Reason = "Seed approved leave",
            AppliedOn = DateTime.UtcNow,
            CurrentApproverId = null,
            ApprovedBy = null,
            ApprovedOn = DateTime.UtcNow,
            IsDeleted = false
        };

        db.LeaveRequests.Add(leaveRequest);
        return leaveRequest;
    }

    public static AttendanceSession SeedAttendanceSession(AppDbContext db, Guid employeeId, DateTime date, TimeSpan workedDuration)
    {
        var checkIn = date.Date.AddHours(9);
        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Date = date.Date,
            CheckInTime = checkIn,
            CheckOutTime = checkIn.Add(workedDuration),
            CreatedAt = DateTime.UtcNow
        };

        db.AttendanceSessions.Add(session);
        return session;
    }
}
