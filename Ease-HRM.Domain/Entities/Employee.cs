namespace Ease_HRM.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid OrgUnitId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
}