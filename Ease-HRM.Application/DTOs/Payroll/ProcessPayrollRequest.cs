namespace Ease_HRM.Application.DTOs.Payroll;

public class ProcessPayrollRequest
{
    public Guid PayrollId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}