namespace Ease_HRM.Application.DTOs.Attendance;

public class UpdateAttendancePolicyRequest
{
    public Guid PolicyId { get; set; }
    public decimal FullDayHours { get; set; }
    public decimal HalfDayHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? ChangeReason { get; set; }
}
