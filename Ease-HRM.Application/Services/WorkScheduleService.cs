using Ease_HRM.Application.DTOs.WorkSchedules;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class WorkScheduleService : IWorkScheduleService
{
    private readonly IWorkScheduleRepository _workScheduleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public WorkScheduleService(IWorkScheduleRepository workScheduleRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _workScheduleRepository = workScheduleRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<WorkSchedule> CreateAsync(CreateWorkScheduleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateScope(request.EmployeeId, request.OrgUnitId);
        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);
        ValidateWeights(request.MondayWeight, request.TuesdayWeight, request.WednesdayWeight, request.ThursdayWeight, request.FridayWeight, request.SaturdayWeight, request.SundayWeight);

        var hasOverlap = await _workScheduleRepository.HasOverlappingScheduleAsync(
            request.EmployeeId,
            request.OrgUnitId,
            request.EffectiveFrom,
            request.EffectiveTo,
            null,
            cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Overlapping work schedule exists for the selected scope.");
        }

        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var schedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            OrgUnitId = request.OrgUnitId,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            MondayWeight = request.MondayWeight,
            TuesdayWeight = request.TuesdayWeight,
            WednesdayWeight = request.WednesdayWeight,
            ThursdayWeight = request.ThursdayWeight,
            FridayWeight = request.FridayWeight,
            SaturdayWeight = request.SaturdayWeight,
            SundayWeight = request.SundayWeight,
            ShiftCode = request.ShiftCode?.Trim(),
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = request.ChangeReason?.Trim()
        };

        await _workScheduleRepository.AddAsync(schedule, cancellationToken);
        await _workScheduleRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.WorkSchedule, schedule.Id, "Work schedule created", cancellationToken);

        return schedule;
    }

    public async Task<WorkSchedule> UpdateAsync(Guid id, UpdateWorkScheduleRequest request, CancellationToken cancellationToken = default)
    {
        ValidationHelper.RequireGuid(id, "WorkScheduleId");
        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);
        ValidateWeights(request.MondayWeight, request.TuesdayWeight, request.WednesdayWeight, request.ThursdayWeight, request.FridayWeight, request.SaturdayWeight, request.SundayWeight);

        var existingSchedule = await _workScheduleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Work schedule not found.");

        ValidateScope(existingSchedule.EmployeeId, existingSchedule.OrgUnitId);

        var hasOverlap = await _workScheduleRepository.HasOverlappingScheduleAsync(
            existingSchedule.EmployeeId,
            existingSchedule.OrgUnitId,
            request.EffectiveFrom,
            request.EffectiveTo,
            id,
            cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Overlapping work schedule exists for the selected scope.");
        }

        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        existingSchedule.IsActive = false;
        existingSchedule.UpdatedBy = actorId;
        existingSchedule.UpdatedAt = now;
        existingSchedule.ChangeReason = "Superseded by new version";

        var newSchedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = existingSchedule.EmployeeId,
            OrgUnitId = existingSchedule.OrgUnitId,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            MondayWeight = request.MondayWeight,
            TuesdayWeight = request.TuesdayWeight,
            WednesdayWeight = request.WednesdayWeight,
            ThursdayWeight = request.ThursdayWeight,
            FridayWeight = request.FridayWeight,
            SaturdayWeight = request.SaturdayWeight,
            SundayWeight = request.SundayWeight,
            ShiftCode = request.ShiftCode?.Trim(),
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = request.ChangeReason?.Trim()
        };

        await _workScheduleRepository.AddAsync(newSchedule, cancellationToken);

        await _workScheduleRepository.SaveChangesAsync(cancellationToken);

        var reason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "N/A" : request.ChangeReason.Trim();
        var details = $"WorkSchedule updated (OldId: {existingSchedule.Id} → NewId: {newSchedule.Id}, Reason: {reason})";

        await _auditLogService.LogAsync(AuditActions.Update, AuditEntities.WorkSchedule, newSchedule.Id, details, cancellationToken);

        return newSchedule;
    }

    private static void ValidateScope(Guid? employeeId, Guid? orgUnitId)
    {
        if (employeeId.HasValue && employeeId.Value == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is invalid.");
        }

        if (orgUnitId.HasValue && orgUnitId.Value == Guid.Empty)
        {
            throw new ArgumentException("OrgUnitId is invalid.");
        }

        if (employeeId.HasValue && orgUnitId.HasValue)
        {
            throw new ArgumentException("Work schedule scope must be either employee-level, org-level, or global.");
        }
    }

    private static void ValidateDateRange(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
        {
            throw new ArgumentException("EffectiveTo cannot be before EffectiveFrom.");
        }
    }

    private static void ValidateWeights(params decimal[] weights)
    {
        foreach (var weight in weights)
        {
            if (weight < 0m || weight > 1m)
            {
                throw new ArgumentException("Work day weight must be between 0 and 1.");
            }
        }
    }
}
