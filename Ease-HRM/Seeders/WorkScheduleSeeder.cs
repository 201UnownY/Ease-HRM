using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Api.Seeders;

public static class WorkScheduleSeeder
{
    public static async Task SeedDefaultGlobalScheduleAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var hasGlobal = await context.WorkSchedules
            .AsNoTracking()
            .Where(x =>
                x.EmployeeId == null &&
                x.OrgUnitId == null)
            .Where(x => x.EffectiveTo == null)
            .AnyAsync(cancellationToken);

        if (hasGlobal)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var schedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = null,
            OrgUnitId = null,
            EffectiveFrom = now.Date,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            MondayWeight = 1m,
            TuesdayWeight = 1m,
            WednesdayWeight = 1m,
            ThursdayWeight = 1m,
            FridayWeight = 1m,
            SaturdayWeight = 0m,
            SundayWeight = 0m,
            ShiftCode = null,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty,
            ChangeReason = "Default global schedule seed"
        };

        schedule.ValidateScope();
        schedule.ValidateWeights();
        schedule.ValidateVersioning();

        await context.WorkSchedules.AddAsync(schedule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
