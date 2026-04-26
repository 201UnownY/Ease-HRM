namespace Ease_HRM.Application.DTOs.Employees;

public class UpdateEmployeeOrgUnitRequest
{
    public Guid EmployeeId { get; set; }
    public Guid OrgUnitId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}