namespace Ease_HRM.Domain.Entities;

public class SalaryStructure
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; }
}