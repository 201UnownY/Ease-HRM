namespace Ease_HRM.Application.DTOs.Payroll;

public class CreateSalaryStructureRequest
{
    public Guid EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
}