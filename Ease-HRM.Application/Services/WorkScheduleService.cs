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
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var schedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            OrgUnitId = request.OrgUnitId,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date,
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

        schedule.ValidateScope();
        schedule.ValidateWeights();
        schedule.ValidateVersioning();

        await _workScheduleRepository.ExecuteInTransactionAsync(async ct =>
        {
            var hasOverlap = await _workScheduleRepository.HasOverlappingScheduleAsync(
                request.EmployeeId,
                request.OrgUnitId,
                request.EffectiveFrom,
                request.EffectiveTo,
                null,
                ct);

            if (hasOverlap)
            {
                throw new InvalidOperationException("Overlapping work schedule exists for the selected scope.");
            }

            await _workScheduleRepository.AddAsync(schedule, ct);
            await _workScheduleRepository.SaveChangesAsync(ct);
            await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.WorkSchedule, schedule.Id, "Work schedule created", ct);
        }, cancellationToken);

        return schedule;
    }

    public async Task<WorkSchedule> UpdateAsync(Guid id, UpdateWorkScheduleRequest request, CancellationToken cancellationToken = default)
    {
        ValidationHelper.RequireGuid(id, "WorkScheduleId");

        var existingSchedule = await _workScheduleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Work schedule not found.");

        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var newSchedule = new WorkSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = existingSchedule.EmployeeId,
            OrgUnitId = existingSchedule.OrgUnitId,
            EffectiveFrom = request.EffectiveFrom.Date,
            EffectiveTo = request.EffectiveTo?.Date,
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

        newSchedule.ValidateScope();
        newSchedule.ValidateWeights();
        newSchedule.ValidateVersioning();

        await _workScheduleRepository.ExecuteInTransactionAsync(async ct =>
        {
            existingSchedule.Supersede(request.EffectiveFrom, actorId);
            existingSchedule.ValidateVersioning();

            var hasOverlap = await _workScheduleRepository.HasOverlappingScheduleAsync(
                existingSchedule.EmployeeId,
                existingSchedule.OrgUnitId,
                request.EffectiveFrom,
                request.EffectiveTo,
                id,
                ct);

            if (hasOverlap)
            {
                throw new InvalidOperationException("Overlapping work schedule exists for the selected scope.");
            }

            await _workScheduleRepository.AddAsync(newSchedule, ct);
            await _workScheduleRepository.SaveChangesAsync(ct);

            var reason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "N/A" : request.ChangeReason.Trim();
            var details = $"WorkSchedule updated (OldId: {existingSchedule.Id} → NewId: {newSchedule.Id}, Reason: {reason})";
            await _auditLogService.LogAsync(AuditActions.Update, AuditEntities.WorkSchedule, newSchedule.Id, details, ct);
        }, cancellationToken);

        return newSchedule;
    }
}
