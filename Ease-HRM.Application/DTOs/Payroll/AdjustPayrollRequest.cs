namespace Ease_HRM.Application.DTOs.Payroll;

public class AdjustPayrollRequest
{
    public Guid PayrollId { get; set; }
    public decimal LeaveDeduction { get; set; }
    public decimal AttendanceDeduction { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}