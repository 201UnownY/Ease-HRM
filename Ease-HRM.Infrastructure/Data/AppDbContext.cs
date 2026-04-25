using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<AttendancePolicy> AttendancePolicies => Set<AttendancePolicy>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.RoleId).IsRequired();

            entity.HasIndex(x => new { x.UserId, x.RoleId })
                .IsUnique();

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RoleId).IsRequired();
            entity.Property(x => x.PermissionId).IsRequired();

            entity.HasIndex(x => new { x.RoleId, x.PermissionId })
                .IsUnique();

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.PerformedAt).IsRequired();

            entity.Property(x => x.Details)
                .HasMaxLength(2000);

            entity.HasIndex(x => x.PerformedAt);
            entity.HasIndex(x => new { x.EntityName, x.EntityId });
            entity.HasIndex(x => x.PerformedBy);
        });

        builder.Entity<OrgUnit>(entity =>
        {
            entity.ToTable("OrgUnits");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Level).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne<OrgUnit>()
                .WithMany()
                .HasForeignKey(x => x.ParentOrgUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(x => x.JoinDate).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.OrgUnitId);
            entity.HasIndex(x => x.ManagerId);
            entity.HasIndex(x => new { x.FirstName, x.LastName });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<OrgUnit>()
                .WithMany()
                .HasForeignKey(x => x.OrgUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkSchedule>(entity =>
        {
            entity.ToTable("WorkSchedules");

            entity.HasKey(x => x.Id);

            // Core fields
            entity.Property(x => x.EffectiveFrom).IsRequired();
            entity.Property(x => x.EffectiveTo).IsRequired(false);

            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();

            entity.Property(x => x.ChangeReason)
                .HasMaxLength(500);

            // Work day weights (0 to 1)
            entity.Property(x => x.MondayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.TuesdayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.WednesdayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.ThursdayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.FridayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.SaturdayWeight).HasPrecision(4, 2).IsRequired();
            entity.Property(x => x.SundayWeight).HasPrecision(4, 2).IsRequired();

            entity.Property(x => x.ShiftCode)
                .HasMaxLength(50);

            // =========================
            // INDEXES (aligned with queries)
            // =========================

            // Main effective lookup (covers range + ordering)
            entity.HasIndex(x => new { x.EmployeeId, x.OrgUnitId, x.EffectiveFrom, x.EffectiveTo, x.CreatedAt })
                .HasDatabaseName("IX_WorkSchedules_EffectiveScope");

            // Faster lookup for frequent queries
            entity.HasIndex(x => new { x.EmployeeId, x.OrgUnitId, x.EffectiveFrom, x.CreatedAt })
                .HasDatabaseName("IX_WorkSchedules_Lookup");

            // Optional deterministic tie-break safety
            entity.HasIndex(x => new { x.EmployeeId, x.OrgUnitId, x.EffectiveFrom, x.CreatedAt, x.Id })
                .HasDatabaseName("IX_WorkSchedules_Deterministic");

            // =========================
            // CONSTRAINTS (data integrity)
            // =========================

            // Ensure valid date range
            entity.HasCheckConstraint(
                "CK_WorkSchedules_EffectiveRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");

            // Ensure valid scope (cannot have both Employee + OrgUnit)
            entity.HasCheckConstraint(
                "CK_WorkSchedules_Scope",
                "NOT ([EmployeeId] IS NOT NULL AND [OrgUnitId] IS NOT NULL)");

            // =========================
            // RELATIONSHIPS
            // =========================

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<OrgUnit>()
                .WithMany()
                .HasForeignKey(x => x.OrgUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // DESIGN NOTE
            // =========================
            // Versioned entity:
            // - No in-place updates
            // - New records inserted on change
            // - Selection based on:
            //     EffectiveFrom/To + scope + CreatedAt (tie-break)
        });

        builder.Entity<Holiday>(entity =>
        {
            entity.ToTable("Holidays");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => new { x.Date, x.OrgUnitId })
                .IsUnique();

            entity.HasIndex(x => x.Date);

            entity.HasOne<OrgUnit>()
                .WithMany()
                .HasForeignKey(x => x.OrgUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("LeaveTypes");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.DefaultDays).IsRequired();
            entity.Property(x => x.Weight).IsRequired();
            entity.Property(x => x.IsPaid).IsRequired();
        });

        builder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("LeaveBalances");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year })
                .IsUnique();

            entity.Property(x => x.Year).IsRequired();
            entity.Property(x => x.Allocated).IsRequired();
            entity.Property(x => x.Used).IsRequired();
            entity.Property(x => x.CarryForward).IsRequired();
            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<LeaveType>()
                .WithMany()
                .HasForeignKey(x => x.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.EndDate).IsRequired();
            entity.Property(x => x.AppliedOn).IsRequired();
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasIndex(x => x.EmployeeId);
            entity.HasIndex(x => x.LeaveTypeId);
            entity.HasIndex(x => x.ApprovedBy);
            entity.HasIndex(x => x.CurrentApproverId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.AppliedOn);
            entity.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<LeaveType>()
                .WithMany()
                .HasForeignKey(x => x.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttendancePolicy>(entity =>
        {
            entity.ToTable("AttendancePolicies");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullDayHours).IsRequired();
            entity.Property(x => x.HalfDayHours).IsRequired();
            entity.Property(x => x.EffectiveFrom).IsRequired();
            entity.Property(x => x.EffectiveTo).IsRequired(false);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
            entity.Property(x => x.ChangeReason).HasMaxLength(500);

            entity.HasCheckConstraint(
                "CK_AttendancePolicies_EffectiveRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");

            entity.HasIndex(x => x.EffectiveFrom);
            entity.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo, x.CreatedAt })
                .HasDatabaseName("IX_AttendancePolicies_Effective");
        });

        builder.Entity<AttendanceSession>(entity =>
        {
            entity.ToTable("AttendanceSessions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CheckInTime).IsRequired();
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => new { x.EmployeeId, x.Date });
            entity.HasIndex(x => new { x.EmployeeId, x.CheckOutTime });
            entity.HasIndex(x => x.EmployeeId)
                .HasFilter("[CheckOutTime] IS NULL")
                .IsUnique();

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SalaryStructure>(entity =>
        {
            entity.ToTable("SalaryStructures");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.BaseSalary).IsRequired();
            entity.Property(x => x.HRA).IsRequired();
            entity.Property(x => x.Allowances).IsRequired();
            entity.Property(x => x.Deductions).IsRequired();
            entity.Property(x => x.EffectiveFrom).IsRequired();
            entity.Property(x => x.EffectiveTo).IsRequired(false);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
            entity.Property(x => x.ChangeReason).HasMaxLength(500);
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasCheckConstraint(
                "CK_SalaryStructures_EffectiveRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");

            entity.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
            entity.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveTo, x.CreatedAt })
                .HasDatabaseName("IX_SalaryStructures_Effective");

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payroll>(entity =>
        {
            entity.ToTable("Payrolls");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Year).IsRequired();
            entity.Property(x => x.Month).IsRequired();

            entity.Property(x => x.BaseSalary).IsRequired();
            entity.Property(x => x.HRA).IsRequired();
            entity.Property(x => x.Allowances).IsRequired();

            entity.Property(x => x.LeaveDeduction).IsRequired();
            entity.Property(x => x.AttendanceDeduction).IsRequired();
            entity.Property(x => x.NetSalary).IsRequired();

            entity.Property(x => x.GeneratedAt).IsRequired();
            entity.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasIndex(x => new { x.EmployeeId, x.Year, x.Month })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasQueryFilter(x => !x.IsDeleted);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(builder);
    }
}