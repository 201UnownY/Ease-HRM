namespace Ease_HRM.Application.DTOs.LeaveRequests;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime AppliedOn { get; set; }
    public Guid? CurrentApproverId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
}