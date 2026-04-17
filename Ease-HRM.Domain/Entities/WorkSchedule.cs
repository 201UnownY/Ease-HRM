namespace Ease_HRM.Domain.Entities;

public class WorkSchedule
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? OrgUnitId { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public decimal MondayWeight { get; set; }
    public decimal TuesdayWeight { get; set; }
    public decimal WednesdayWeight { get; set; }
    public decimal ThursdayWeight { get; set; }
    public decimal FridayWeight { get; set; }
    public decimal SaturdayWeight { get; set; }
    public decimal SundayWeight { get; set; }

    public string? ShiftCode { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public string? ChangeReason { get; set; }
}
