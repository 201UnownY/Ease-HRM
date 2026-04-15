namespace Ease_HRM.Application.DTOs.LeaveTypes;

public class LeaveTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultDays { get; set; }
    public decimal Weight { get; set; }
    public bool IsPaid { get; set; }
}