using Ease_HRM.Application.DTOs.Attendance;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAuditLogService _auditLogService;

    public AttendanceService(IAttendanceRepository attendanceRepository, IAuditLogService auditLogService)
    {
        _attendanceRepository = attendanceRepository;
        _auditLogService = auditLogService;
    }

    public async Task CheckInAsync(CheckInRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, "EmployeeId");

        var now = DateTime.UtcNow;

        var employeeExists = await _attendanceRepository.EmployeeExistsAsync(employeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var activeSession = await _attendanceRepository.GetActiveSessionAsync(employeeId, cancellationToken);
        if (activeSession is not null)
        {
            throw new InvalidOperationException("Employee already has an active session.");
        }

        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            CheckInTime = now,
            Date = now.Date,
            CheckOutTime = null,
            CreatedAt = now
        };

        await _attendanceRepository.AddSessionAsync(session, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.AttendanceSession, session.Id, "Attendance check-in", cancellationToken);
    }

    public async Task CheckOutAsync(CheckOutRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, "EmployeeId");

        var employeeExists = await _attendanceRepository.EmployeeExistsAsync(employeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var activeSession = await _attendanceRepository.GetActiveSessionAsync(employeeId, cancellationToken);
        if (activeSession is null)
        {
            throw new InvalidOperationException("No active session found.");
        }

        if (activeSession.CheckOutTime != null)
        {
            throw new InvalidOperationException("Session already checked out.");
        }

        var now = DateTime.UtcNow;
        activeSession.CheckOutTime = now;

        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Update, AuditEntities.AttendanceSession, activeSession.Id, "Attendance check-out", cancellationToken);
    }

    public async Task<AttendanceRecordDto> GetDailySummaryAsync(Guid employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var sessionDate = date.Date;
        var sessions = await _attendanceRepository.GetSessionsByDateAsync(employeeId, sessionDate, cancellationToken);
        var totalHours = AttendanceCalculator.CalculateTotalHours(sessions);

        var policy = await _attendanceRepository.GetEffectivePolicyAsync(sessionDate, cancellationToken);

        var status = AttendanceStatus.Absent;
        if (policy is not null)
        {
            var hasLeave = await _attendanceRepository.HasApprovedLeaveAsync(employeeId, sessionDate, cancellationToken);
            status = hasLeave ? AttendanceStatus.Leave : DetermineStatus(totalHours, policy);
        }

        return new AttendanceRecordDto
        {
            EmployeeId = employeeId,
            Date = sessionDate,
            TotalHours = totalHours,
            Status = status.ToString()
        };
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _attendanceRepository.GetAllSessionsAsync(cancellationToken);
        var leaveRanges = await _attendanceRepository.GetApprovedLeaveRangesAsync(cancellationToken);
        var leaveLookup = leaveRanges
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(r => (r.Start, r.End)).OrderBy(r => r.Start).ToList());

        var grouped = sessions.GroupBy(s => new { s.EmployeeId, s.Date });
        var policy = await _attendanceRepository.GetEffectivePolicyAsync(DateTime.UtcNow.Date, cancellationToken);
        if (policy is null)
        {
            throw new InvalidOperationException("Attendance policy not configured.");
        }

        var results = new List<AttendanceRecordDto>();
        foreach (var group in grouped)
        {
            var empId = group.Key.EmployeeId;
            var dt = group.Key.Date;
            var totalHours = AttendanceCalculator.CalculateTotalHours(group);
            var hasLeave = leaveLookup.TryGetValue(empId, out var ranges) && IsDateInRanges(ranges, dt.Date);
            var status = hasLeave ? AttendanceStatus.Leave : DetermineStatus(totalHours, policy);

            results.Add(new AttendanceRecordDto
            {
                EmployeeId = empId,
                Date = dt,
                TotalHours = totalHours,
                Status = status.ToString()
            });
        }

        return results.AsReadOnly();
    }

    private static bool IsDateInRanges(List<(DateTime Start, DateTime End)> ranges, DateTime date)
    {
        var target = date.Date;

        var low = 0;
        var high = ranges.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            var range = ranges[mid];

            if (target < range.Start)
            {
                high = mid - 1;
            }
            else if (target > range.End)
            {
                low = mid + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static AttendanceStatus DetermineStatus(decimal totalHours, AttendancePolicy policy)
    {
        if (totalHours >= policy.FullDayHours)
            return AttendanceStatus.Present;

        if (totalHours >= policy.HalfDayHours)
            return AttendanceStatus.HalfDay;

        return AttendanceStatus.Absent;
    }
}