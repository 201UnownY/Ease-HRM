namespace Ease_HRM.Application.DTOs.Employees;

public class ChangeManagerRequest
{
    public Guid EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}