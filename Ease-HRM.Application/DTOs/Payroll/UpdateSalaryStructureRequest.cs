namespace Ease_HRM.Application.DTOs.Payroll;

public class UpdateSalaryStructureRequest
{
    public Guid SalaryStructureId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? ChangeReason { get; set; }
}
