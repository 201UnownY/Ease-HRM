using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestService(ILeaveRequestRepository leaveRequestRepository, ICurrentUserService currentUserService)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _currentUserService = currentUserService;
    }

    public async Task<LeaveRequestDto> ApplyLeaveAsync(ApplyLeaveRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId is required.");

        if (request.LeaveTypeId == Guid.Empty)
            throw new ArgumentException("LeaveTypeId is required.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required.");

        if (request.StartDate > request.EndDate)
            throw new ArgumentException("StartDate cannot be after EndDate.");

        var today = DateTime.UtcNow.Date;

        if (request.StartDate.Date < today)
            throw new ArgumentException("StartDate cannot be in the past.");

        if (request.EndDate.Date < today)
            throw new ArgumentException("EndDate cannot be in the past.");

        var duration = (request.EndDate.Date - request.StartDate.Date).TotalDays + 1;
        if (duration <= 0)
            throw new ArgumentException("Invalid leave duration.");

        if (!await _leaveRequestRepository.EmployeeExistsAsync(request.EmployeeId, cancellationToken))
            throw new InvalidOperationException("Employee not found.");

        if (!await _leaveRequestRepository.LeaveTypeExistsAsync(request.LeaveTypeId, cancellationToken))
            throw new InvalidOperationException("LeaveType not found.");

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = LeaveStatus.Pending,
            Reason = request.Reason.Trim(),
            AppliedOn = DateTime.UtcNow,
            ApprovedBy = null,
            ApprovedOn = null
        };

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Status = leaveRequest.Status.ToString(),
            Reason = leaveRequest.Reason,
            AppliedOn = leaveRequest.AppliedOn,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedOn = leaveRequest.ApprovedOn
        };
    }

    public async Task<LeaveRequestDto> ApproveLeaveAsync(ApproveLeaveRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LeaveRequestId == Guid.Empty)
        {
            throw new ArgumentException("LeaveRequestId is required.");
        }

        var approverId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
        {
            throw new InvalidOperationException("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be approved.");
        }

        if (approverId == leaveRequest.EmployeeId)
        {
            throw new InvalidOperationException("Employee cannot approve their own leave.");
        }

        leaveRequest.Status = LeaveStatus.Approved;
        leaveRequest.ApprovedBy = approverId;
        leaveRequest.ApprovedOn = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Status = leaveRequest.Status.ToString(),
            Reason = leaveRequest.Reason,
            AppliedOn = leaveRequest.AppliedOn,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedOn = leaveRequest.ApprovedOn
        };
    }

    public async Task<LeaveRequestDto> RejectLeaveAsync(RejectLeaveRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LeaveRequestId == Guid.Empty)
        {
            throw new ArgumentException("LeaveRequestId is required.");
        }

        var approverId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
        {
            throw new InvalidOperationException("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be rejected.");
        }

        leaveRequest.Status = LeaveStatus.Rejected;
        leaveRequest.ApprovedBy = approverId;
        leaveRequest.ApprovedOn = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        return new LeaveRequestDto
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Status = leaveRequest.Status.ToString(),
            Reason = leaveRequest.Reason,
            AppliedOn = leaveRequest.AppliedOn,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedOn = leaveRequest.ApprovedOn
        };
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllLeaveRequestsAsync(CancellationToken cancellationToken = default)
    {
        var leaveRequests = await _leaveRequestRepository.GetAllAsync(cancellationToken);

        return leaveRequests
            .Select(x => new LeaveRequestDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                LeaveTypeId = x.LeaveTypeId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status.ToString(),
                Reason = x.Reason,
                AppliedOn = x.AppliedOn,
                ApprovedBy = x.ApprovedBy,
                ApprovedOn = x.ApprovedOn
            })
            .ToList()
            .AsReadOnly();
    }
}