namespace Ease_HRM.Domain.Entities;

public class WorkSchedule
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? OrgUnitId { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
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

    public void ValidateVersioning()
    {
        if (EffectiveFrom == default)
        {
            throw new InvalidOperationException("EffectiveFrom is required.");
        }

        if (EffectiveTo.HasValue && EffectiveTo.Value.Date < EffectiveFrom.Date)
        {
            throw new InvalidOperationException("EffectiveTo cannot be before EffectiveFrom.");
        }
    }

    public void ValidateScope()
    {
        if (EmployeeId.HasValue && OrgUnitId.HasValue)
        {
            throw new InvalidOperationException("WorkSchedule cannot have both EmployeeId and OrgUnitId.");
        }
    }

    public void ValidateWeights()
    {
        var weights = new[]
        {
            MondayWeight, TuesdayWeight, WednesdayWeight,
            ThursdayWeight, FridayWeight, SaturdayWeight, SundayWeight
        };

        if (weights.Any(w => w < 0m || w > 1m))
        {
            throw new InvalidOperationException("Work day weights must be between 0 and 1.");
        }
    }

    public void ValidateContinuity(DateTime newDate)
    {
        if (EffectiveTo.HasValue)
        {
            throw new InvalidOperationException("Cannot supersede a closed WorkSchedule version.");
        }

        if (newDate <= EffectiveFrom.Date)
        {
            throw new InvalidOperationException("New effective date must be after current version.");
        }
    }

    private const string SupersededReason = "Superseded by new version";
    public void Supersede(DateTime newEffectiveFrom, Guid actorId)
    {
        var newDate = newEffectiveFrom.Date;

        ValidateContinuity(newDate);

        EffectiveTo = newDate.AddDays(-1);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = actorId;
        ChangeReason = SupersededReason;
    }
}
