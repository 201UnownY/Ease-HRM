namespace Ease_HRM.Domain.Entities;

public class AttendancePolicy
{
    public Guid Id { get; set; }
    public decimal FullDayHours { get; set; }
    public decimal HalfDayHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public bool IsActive { get; set; }
}