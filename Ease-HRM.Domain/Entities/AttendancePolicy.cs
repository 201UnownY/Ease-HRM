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

        if (HalfDayHours >= FullDayHours)
        {
            throw new InvalidOperationException("HalfDayHours must be less than FullDayHours.");
        }
    }

    public void Supersede(DateTime newEffectiveFrom, Guid actorId)
    {
        if (newEffectiveFrom.Date <= EffectiveFrom.Date)
        {
            throw new InvalidOperationException("New effective date must be after current version.");
        }

        EffectiveTo = newEffectiveFrom.Date.AddDays(-1);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = actorId;
        ChangeReason = "Superseded by new version";
    }
}