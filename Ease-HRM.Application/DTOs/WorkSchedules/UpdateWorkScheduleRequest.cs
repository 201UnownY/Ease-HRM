namespace Ease_HRM.Application.DTOs.WorkSchedules;

public class UpdateWorkScheduleRequest
{
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public decimal MondayWeight { get; set; }
    public decimal TuesdayWeight { get; set; }
    public decimal WednesdayWeight { get; set; }
    public decimal ThursdayWeight { get; set; }
    public decimal FridayWeight { get; set; }
    public decimal SaturdayWeight { get; set; }
    public decimal SundayWeight { get; set; }

    public string? ShiftCode { get; set; }
    public string? ChangeReason { get; set; }
}
