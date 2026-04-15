namespace Ease_HRM.Domain.Entities;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal Allocated { get; set; }
    public decimal Used { get; set; }
    public decimal CarryForward { get; set; }
}