using Ease_HRM.Application.Constants;
using Ease_HRM.Application.DTOs.Attendance;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class AttendancePolicyService : IAttendancePolicyService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public AttendancePolicyService(IAttendanceRepository attendanceRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _attendanceRepository = attendanceRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<AttendancePolicy> CreateAsync(CreateAttendancePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var effectiveFrom = request.EffectiveFrom.Date;
        var now = DateTime.UtcNow;
        var actorId = _currentUserService.UserId ?? Guid.Empty;

        var policy = new AttendancePolicy
        {
            Id = Guid.NewGuid(),
            FullDayHours = request.FullDayHours,
            HalfDayHours = request.HalfDayHours,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "Initial creation" : request.ChangeReason.Trim()
        };

        policy.ValidateVersioning();

        await _attendanceRepository.ExecuteInTransactionAsync(async ct =>
        {
            var overlap = await _attendanceRepository.HasOverlappingPolicyAsync(effectiveFrom, null, null, ct);
            if (overlap)
            {
                throw new InvalidOperationException("Overlapping attendance policy exists.");
            }

            await _attendanceRepository.AddPolicyAsync(policy, ct);
            await _attendanceRepository.SaveChangesAsync(ct);
            await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.AttendancePolicy, policy.Id, "Attendance policy created", ct);
        }, cancellationToken);

        return policy;
    }

    public async Task<AttendancePolicy> UpdateAsync(UpdateAttendancePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policyId = ValidationHelper.RequireGuid(request.PolicyId, nameof(request.PolicyId));

        var existing = await _attendanceRepository.GetPolicyByIdAsync(policyId, cancellationToken)
            ?? throw new InvalidOperationException("Attendance policy not found.");

        var effectiveFrom = request.EffectiveFrom.Date;

        var overlap = await _attendanceRepository.HasOverlappingPolicyAsync(effectiveFrom, null, policyId, cancellationToken);
        if (overlap)
        {
            throw new InvalidOperationException("Overlapping attendance policy exists.");
        }

        var now = DateTime.UtcNow;
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var reason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "Policy updated" : request.ChangeReason.Trim();

        existing.EffectiveTo = effectiveFrom.AddDays(-1);
        existing.UpdatedAt = now;
        existing.UpdatedBy = actorId;
        existing.ChangeReason = "Superseded by new version";

        var newPolicy = new AttendancePolicy
        {
            Id = Guid.NewGuid(),
            FullDayHours = request.FullDayHours,
            HalfDayHours = request.HalfDayHours,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = reason
        };

        newPolicy.ValidateVersioning();

        await _attendanceRepository.ExecuteInTransactionAsync(async ct =>
        {
            await _attendanceRepository.SaveChangesAsync(ct);
            await _attendanceRepository.AddPolicyAsync(newPolicy, ct);
            await _attendanceRepository.SaveChangesAsync(ct);

            var details = $"AttendancePolicy updated (OldId: {existing.Id} → NewId: {newPolicy.Id}, Reason: {reason})";
            await _auditLogService.LogAsync(AuditActions.Update, AuditEntities.AttendancePolicy, newPolicy.Id, details, ct);
        }, cancellationToken);

        return newPolicy;
    }
}
