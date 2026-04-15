namespace Ease_HRM.Application.DTOs.Payroll;

public class PayrollDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Allowances { get; set; }
    public decimal LeaveDeduction { get; set; }
    public decimal AttendanceDeduction { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedAt { get; set; }
}