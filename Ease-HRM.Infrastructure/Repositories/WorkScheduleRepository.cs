using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Ease_HRM.Infrastructure.Repositories;

public class WorkScheduleRepository : IWorkScheduleRepository
{
    private readonly AppDbContext _context;

    public WorkScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsWorkingDay(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var orgUnitId = await GetOrgUnitId(employeeId, cancellationToken);

        var effectiveSchedule = await GetEffectiveScheduleAsync(employeeId, orgUnitId, date.Date, cancellationToken);
        if (effectiveSchedule is null)
        {
            return GetDefaultWeight(date.DayOfWeek) > 0;
        }

        return GetDayWeight(effectiveSchedule, date.DayOfWeek) > 0;
    }

    public async Task<int> GetWorkingDays(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var weights = await GetWorkingDateWeights(employeeId, year, month, cancellationToken);
        return weights.Count(x => x.Value > 0);
    }

    public async Task<HashSet<DateTime>> GetWorkingDateSet(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var weights = await GetWorkingDateWeights(employeeId, year, month, cancellationToken);
        return weights
            .Where(x => x.Value > 0)
            .Select(x => x.Key)
            .ToHashSet();
    }

    public async Task<Dictionary<DateTime, decimal>> GetWorkingDateWeights(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthEndExclusive = monthEnd.AddDays(1);

        var orgUnitId = await GetOrgUnitId(employeeId, cancellationToken);

        // TODO: extend orgUnitId resolution to inherited parent-org holiday/schedule scopes when org tree traversal abstraction is introduced.

        var rawHolidays = await _context.Holidays
            .AsNoTracking()
            .Where(x => x.Date >= monthStart && x.Date < monthEndExclusive && (x.OrgUnitId == null || x.OrgUnitId == orgUnitId))
            .Select(x => new { x.Date, x.OrgUnitId })
            .ToListAsync(cancellationToken);

        var holidayDates = rawHolidays
            .GroupBy(x => x.Date.Date)
            .Select(group => group
                .OrderByDescending(x => x.OrgUnitId.HasValue && x.OrgUnitId == orgUnitId)
                .First().Date.Date)
            .ToHashSet();

        var schedules = await _context.Set<WorkSchedule>()
            .AsNoTracking()
            .Where(ScopeFilter(employeeId, orgUnitId))
            .Where(x =>
                x.EffectiveFrom < monthEndExclusive &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= monthStart))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<DateTime, decimal>();

        for (var currentDate = monthStart; currentDate <= monthEnd; currentDate = currentDate.AddDays(1))
        {
            var date = currentDate.Date;

            var applicable = SelectEffectiveSchedule(schedules, employeeId, orgUnitId, date);

            var weight = applicable is null
                ? GetDefaultWeight(date.DayOfWeek)
                : GetDayWeight(applicable, date.DayOfWeek);

            if (holidayDates.Contains(date))
            {
                weight = 0m;
            }

            if (weight > 0)
            {
                result[date] = weight;
            }
        }

        return result;
    }

    public Task<WorkSchedule?> GetEffectiveScheduleAsync(Guid employeeId, Guid? orgUnitId, DateTime date, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;

        return _context.WorkSchedules
            .AsNoTracking()
            .Where(ScopeFilter(employeeId, orgUnitId))
            .Where(x =>
                x.EffectiveFrom <= targetDate &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= targetDate))
            .OrderByDescending(x => GetScopePriority(x, employeeId, orgUnitId))
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<WorkSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.WorkSchedules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(WorkSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.WorkSchedules.AddAsync(schedule, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasOverlappingScheduleAsync(
        Guid? employeeId,
        Guid? orgUnitId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        Guid? excludeScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        var newStart = effectiveFrom.Date;
        var newEnd = (effectiveTo ?? DateTime.MaxValue.Date).Date;

        return _context.WorkSchedules
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.OrgUnitId == orgUnitId)
            .Where(x => !excludeScheduleId.HasValue || x.Id != excludeScheduleId.Value)
            .AnyAsync(x =>
                x.EffectiveFrom <= newEnd &&
                (x.EffectiveTo ?? DateTime.MaxValue) >= newStart,
                cancellationToken);
    }

    private static bool MatchesScope(WorkSchedule schedule, Guid employeeId, Guid? orgUnitId)
    {
        return schedule.EmployeeId == employeeId ||
               (schedule.EmployeeId == null && orgUnitId.HasValue && schedule.OrgUnitId == orgUnitId.Value) ||
               (schedule.EmployeeId == null && schedule.OrgUnitId == null);
    }

    private static Expression<Func<WorkSchedule, bool>> ScopeFilter(Guid employeeId, Guid? orgUnitId)
    {
        return x =>
            x.EmployeeId == employeeId ||
            (x.EmployeeId == null && orgUnitId.HasValue && x.OrgUnitId == orgUnitId.Value) ||
            (x.EmployeeId == null && x.OrgUnitId == null);
    }

    private async Task<Guid?> GetOrgUnitId(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => (Guid?)x.OrgUnitId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Employee > OrgUnit > Global priority
    private static int GetScopePriority(WorkSchedule schedule, Guid employeeId, Guid? orgUnitId)
    {
        if (schedule.EmployeeId.HasValue && schedule.EmployeeId.Value == employeeId)
        {
            return 3;
        }

        if (!schedule.EmployeeId.HasValue && schedule.OrgUnitId.HasValue && orgUnitId.HasValue && schedule.OrgUnitId.Value == orgUnitId.Value)
        {
            return 2;
        }

        if (!schedule.EmployeeId.HasValue && !schedule.OrgUnitId.HasValue)
        {
            return 1;
        }

        return 0;
    }

    private static decimal GetDayWeight(WorkSchedule schedule, DayOfWeek dayOfWeek)
    {
        var weight = dayOfWeek switch
        {
            DayOfWeek.Monday => schedule.MondayWeight,
            DayOfWeek.Tuesday => schedule.TuesdayWeight,
            DayOfWeek.Wednesday => schedule.WednesdayWeight,
            DayOfWeek.Thursday => schedule.ThursdayWeight,
            DayOfWeek.Friday => schedule.FridayWeight,
            DayOfWeek.Saturday => schedule.SaturdayWeight,
            DayOfWeek.Sunday => schedule.SundayWeight,
            _ => 0m
        };

        return Math.Clamp(weight, 0m, 1m);
    }

    private static decimal GetDefaultWeight(DayOfWeek dayOfWeek)
    {
        return dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0m : 1m;
    }

    private static WorkSchedule? SelectEffectiveSchedule(IEnumerable<WorkSchedule> schedules, Guid employeeId, Guid? orgUnitId, DateTime date)
    {
        return schedules
            .Where(x => x.EffectiveFrom <= date && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date) && MatchesScope(x, employeeId, orgUnitId))
            .OrderByDescending(x => GetScopePriority(x, employeeId, orgUnitId))
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();
    }
}
