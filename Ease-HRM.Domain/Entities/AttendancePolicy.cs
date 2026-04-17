namespace Ease_HRM.Domain.Entities;

public class AttendancePolicy
{
    public Guid Id { get; set; }
    public decimal FullDayHours { get; set; }
    public decimal HalfDayHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public string? ChangeReason { get; set; }
}