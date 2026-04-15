using Ease_HRM.Application.DTOs.Attendance;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task CheckInAsync(CheckInRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.");
        }

        var now = DateTime.UtcNow;

        var employeeExists = await _attendanceRepository.EmployeeExistsAsync(request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var activeSession = await _attendanceRepository.GetActiveSessionAsync(request.EmployeeId, cancellationToken);
        if (activeSession is not null)
        {
            throw new InvalidOperationException("Employee already has an active session.");
        }

        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            CheckInTime = now,
            Date = now.Date,
            CheckOutTime = null,
            CreatedAt = now
        };

        await _attendanceRepository.AddSessionAsync(session, cancellationToken);
        await _attendanceRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CheckOutAsync(CheckOutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.");
        }

        var employeeExists = await _attendanceRepository.EmployeeExistsAsync(request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var activeSession = await _attendanceRepository.GetActiveSessionAsync(request.EmployeeId, cancellationToken);
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

        var policy = await _attendanceRepository.GetActivePolicyAsync(cancellationToken);
        if (policy is null)
        {
            throw new InvalidOperationException("Attendance policy not configured.");
        }

        var sessionDate = activeSession.Date;
        var sessions = await _attendanceRepository.GetSessionsByDateAsync(request.EmployeeId, sessionDate, cancellationToken);

        var totalHours = CalculateTotalHours(sessions);
        var hasLeave = await _attendanceRepository.HasApprovedLeaveAsync(request.EmployeeId, sessionDate, cancellationToken);

        var status = hasLeave 
            ? AttendanceStatus.Leave 
            : DetermineStatus(totalHours, policy);

        var existingRecord = await _attendanceRepository.GetRecordByDateAsync(request.EmployeeId, sessionDate, cancellationToken);

        if (existingRecord is not null)
        {
            existingRecord.TotalHours = totalHours;
            existingRecord.Status = status;
        }
        else
        {
            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                Date = sessionDate,
                TotalHours = totalHours,
                Status = status,
                CreatedAt = now
            };

            await _attendanceRepository.AddRecordAsync(record, cancellationToken);
        }

        await _attendanceRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(CancellationToken cancellationToken = default)
    {
        var records = await _attendanceRepository.GetAllRecordsAsync(cancellationToken);

        return records
            .Select(x => new AttendanceRecordDto
            {
                EmployeeId = x.EmployeeId,
                Date = x.Date,
                TotalHours = x.TotalHours,
                Status = x.Status.ToString()
            })
            .ToList()
            .AsReadOnly();
    }

    private static decimal CalculateTotalHours(List<AttendanceSession> sessions)
    {
        decimal total = 0;

        foreach (var session in sessions)
        {
            if (session.CheckOutTime.HasValue)
            {
                var duration = session.CheckOutTime.Value - session.CheckInTime;

                if (duration.TotalSeconds <= 0)
                {
                    continue;
                }

                if (duration.TotalMinutes < 1)
                {
                    continue;
                }

                total += (decimal)duration.TotalHours;
            }
        }

        return Math.Round(total, 2);
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