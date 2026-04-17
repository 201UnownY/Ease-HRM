using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Domain.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AppliedOn { get; set; }
    public Guid? CurrentApproverId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}