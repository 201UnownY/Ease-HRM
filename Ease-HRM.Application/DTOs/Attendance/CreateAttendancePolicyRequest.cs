namespace Ease_HRM.Application.DTOs.Attendance;

public class CreateAttendancePolicyRequest
{
    public decimal FullDayHours { get; set; }
    public decimal HalfDayHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? ChangeReason { get; set; }
}
