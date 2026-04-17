namespace Ease_HRM.Application.Constants;

public static class Permissions
{
    public static class User
    {
        public const string Create = "user.create";
        public const string View = "user.view";
    }

    public static class Role
    {
        public const string Create = "role.create";
        public const string View = "role.view";
    }

    public static class Permission
    {
        public const string Create = "permission.create";
        public const string View = "permission.view";
    }

    public static class UserRole
    {
        public const string Assign = "userrole.assign";
        public const string View = "userrole.view";
    }

    public static class RolePermission
    {
        public const string Assign = "rolepermission.assign";
        public const string View = "rolepermission.view";
    }

    public static class OrgUnit
    {
        public const string Create = "orgunit.create";
        public const string View = "orgunit.view";
    }

    public static class Employee
    {
        public const string Create = "employee.create";
        public const string View = "employee.view";
        public const string Update = "employee.update";
    }

    public static class LeaveType
    {
        public const string Create = "leavetype.create";
        public const string View = "leavetype.view";
    }

    public static class Leave
    {
        public const string Apply = "leave.apply";
        public const string Approve = "leave.approve";
        public const string Reject = "leave.reject";
        public const string View = "leave.view";
    }

    public static class Attendance
    {
        public const string CheckIn = "attendance.checkin";
        public const string CheckOut = "attendance.checkout";
        public const string ManagePolicy = "attendance.manage_policy";
        public const string View = "attendance.view";
    }

    public static class Payroll
    {
        public const string Generate = "payroll.generate";
        public const string View = "payroll.view";
        public const string ManageSalaryStructure = "payroll.manage_salary_structure";
    }
}