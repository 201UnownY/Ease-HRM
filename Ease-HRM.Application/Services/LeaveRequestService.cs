using Ease_HRM.Application.DTOs.LeaveRequests;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Constants;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public LeaveRequestService(ILeaveRequestRepository leaveRequestRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<LeaveRequestDto> ApplyLeaveAsync(ApplyLeaveRequest request, CancellationToken cancellationToken = default)
    {
        var employeeId = ValidationHelper.RequireGuid(request.EmployeeId, "EmployeeId");
        var leaveTypeId = ValidationHelper.RequireGuid(request.LeaveTypeId, "LeaveTypeId");
        var reason = ValidationHelper.RequireString(request.Reason, "Reason");

        if (request.StartDate > request.EndDate)
            throw new ArgumentException("StartDate cannot be after EndDate.");

        var today = DateTime.UtcNow.Date;

        if (request.StartDate.Date < today)
            throw new ArgumentException("StartDate cannot be in the past.");

        if (request.EndDate.Date < today)
            throw new ArgumentException("EndDate cannot be in the past.");

        if (request.StartDate.Year != request.EndDate.Year)
            throw new ArgumentException("Cross-year leave not supported.");

        var duration = CalculateLeaveDays(request.StartDate, request.EndDate);
        if (duration <= 0)
            throw new ArgumentException("Invalid leave duration.");

        var employee = await _leaveRequestRepository.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null)
            throw new InvalidOperationException("Employee not found.");

        if (!await _leaveRequestRepository.LeaveTypeExistsAsync(leaveTypeId, cancellationToken))
            throw new InvalidOperationException("LeaveType not found.");

        if (await _leaveRequestRepository.HasOverlappingLeaveAsync(employeeId, request.StartDate, request.EndDate, cancellationToken))
        {
            throw new InvalidOperationException("Overlapping leave request exists.");
        }

        var leaveBalance = await _leaveRequestRepository.GetLeaveBalanceAsync(employeeId, leaveTypeId, request.StartDate.Year, cancellationToken);
        if (leaveBalance is null)
        {
            throw new InvalidOperationException("Leave balance not configured for the requested year AND leave type.");
        }

        var availableLeave = leaveBalance.Allocated + leaveBalance.CarryForward - leaveBalance.Used;
        if (duration > availableLeave)
        {
            throw new InvalidOperationException("Insufficient leave balance.");
        }

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = LeaveStatus.Pending,
            Reason = reason,
            AppliedOn = DateTime.UtcNow,
            CurrentApproverId = employee.ManagerId,
            ApprovedBy = null,
            ApprovedOn = null,
            IsDeleted = false
        };

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            AuditActions.Create,
            AuditEntities.LeaveRequest,
            leaveRequest.Id,
            "Leave request applied",
            cancellationToken);

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
            CurrentApproverId = leaveRequest.CurrentApproverId,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedOn = leaveRequest.ApprovedOn
        };
    }

    private async Task<bool> IsInHierarchy(Guid employeeId, Guid approverId, CancellationToken ct)
    {
        // TODO: Replace recursive DB calls with preloaded hierarchy chain.
        var hierarchyEmployees = await _leaveRequestRepository.GetHierarchyEmployeesAsync(employeeId, ct);
        var hierarchyMap = hierarchyEmployees.ToDictionary(x => x.Id, x => x);

        if (!hierarchyMap.TryGetValue(employeeId, out var current))
        {
            return false;
        }

        var visited = new HashSet<Guid>();

        while (current?.ManagerId != null)
        {
            if (!visited.Add(current.Id))
            {
                throw new InvalidOperationException("Cycle detected in hierarchy.");
            }

            if (current.ManagerId == approverId)
                return true;

            if (!hierarchyMap.TryGetValue(current.ManagerId.Value, out var manager))
            {
                return false;
            }

            current = manager;
        }

        return false;
    }

    public async Task<LeaveRequestDto> ApproveLeaveAsync(ApproveLeaveRequest request, CancellationToken cancellationToken = default)
    {
        var leaveRequestId = ValidationHelper.RequireGuid(request.LeaveRequestId, "LeaveRequestId");

        var approverId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId, cancellationToken);
        if (leaveRequest is null)
        {
            throw new InvalidOperationException("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be approved.");
        }

        var approverEmp = await _leaveRequestRepository.GetEmployeeByUserIdAsync(approverId, cancellationToken);

        if (approverEmp?.Id == leaveRequest.EmployeeId)
        {
            throw new InvalidOperationException("Employee cannot approve their own leave.");
        }

        var roles = await _currentUserService.GetRolesAsync(cancellationToken);
        bool isHrOrAdmin = roles
            .Any(x => string.Equals(x, SystemRoles.HR, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(x, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase));

        if (!isHrOrAdmin)
        {
            if (approverEmp == null)
            {
                throw new InvalidOperationException("Approver not found.");
            }

            if (!await IsInHierarchy(leaveRequest.EmployeeId, approverEmp.Id, cancellationToken))
            {
                throw new InvalidOperationException("Approver not in reporting hierarchy.");
            }

            if (leaveRequest.CurrentApproverId != approverEmp.Id)
            {
                throw new InvalidOperationException("Current user is not the designated approver for this leave request.");
            }

            if (approverEmp.ManagerId.HasValue)
            {
                var nextManager = await _leaveRequestRepository.GetEmployeeAsync(approverEmp.ManagerId.Value, cancellationToken);
                if (nextManager == null)
                {
                    throw new InvalidOperationException("Invalid hierarchy configuration.");
                }

                leaveRequest.CurrentApproverId = nextManager.Id;
            }
            else
            {
                leaveRequest.Status = LeaveStatus.Approved;
                leaveRequest.ApprovedBy = approverEmp.Id;
                leaveRequest.ApprovedOn = DateTime.UtcNow;
            }
        }
        else
        {
            leaveRequest.Status = LeaveStatus.Approved;
            leaveRequest.ApprovedBy = approverEmp?.Id;
            leaveRequest.ApprovedOn = DateTime.UtcNow;
            leaveRequest.CurrentApproverId = null;
        }

        await _leaveRequestRepository.ExecuteInTransactionAsync(async ct =>
        {
            if (leaveRequest.Status == LeaveStatus.Approved)
            {
                var balance = await _leaveRequestRepository.GetLeaveBalanceAsync(
                    leaveRequest.EmployeeId,
                    leaveRequest.LeaveTypeId,
                    leaveRequest.StartDate.Year,
                    ct);

                if (balance != null)
                {
                    var duration = CalculateLeaveDays(leaveRequest.StartDate, leaveRequest.EndDate);

                    if (balance.Used + duration > balance.Allocated + balance.CarryForward)
                    {
                        throw new InvalidOperationException("Insufficient leave balance.");
                    }

                    balance.Used += duration;
                }
            }

            await _leaveRequestRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        if (leaveRequest.Status == LeaveStatus.Approved)
        {
            await _auditLogService.LogAsync(AuditActions.Approve, AuditEntities.LeaveRequest, leaveRequest.Id, "Leave request approved", cancellationToken);
        }

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
            CurrentApproverId = leaveRequest.CurrentApproverId,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApprovedOn = leaveRequest.ApprovedOn
        };
    }

    public async Task<LeaveRequestDto> RejectLeaveAsync(RejectLeaveRequest request, CancellationToken cancellationToken = default)
    {
        var leaveRequestId = ValidationHelper.RequireGuid(request.LeaveRequestId, "LeaveRequestId");

        var approverId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId, cancellationToken);
        if (leaveRequest is null)
        {
            throw new InvalidOperationException("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be rejected.");
        }

        var approverEmp = await _leaveRequestRepository.GetEmployeeByUserIdAsync(approverId, cancellationToken);

        if (approverEmp?.Id == leaveRequest.EmployeeId)
        {
            throw new InvalidOperationException("Employee cannot reject their own leave.");
        }

        var roles = await _currentUserService.GetRolesAsync(cancellationToken);
        bool isHrOrAdmin = roles
            .Any(x => string.Equals(x, SystemRoles.HR, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(x, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase));

        if (!isHrOrAdmin)
        {
            if (approverEmp == null)
            {
                throw new InvalidOperationException("Approver not found.");
            }

            if (!await IsInHierarchy(leaveRequest.EmployeeId, approverEmp.Id, cancellationToken))
            {
                throw new InvalidOperationException("Approver not in reporting hierarchy.");
            }

            if (leaveRequest.CurrentApproverId != approverEmp.Id)
            {
                throw new InvalidOperationException("Current user is not the designated approver for this leave request.");
            }
        }

        leaveRequest.Status = LeaveStatus.Rejected;
        leaveRequest.ApprovedBy = approverEmp?.Id;
        leaveRequest.ApprovedOn = DateTime.UtcNow;

        await _leaveRequestRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Reject, AuditEntities.LeaveRequest, leaveRequest.Id, "Leave request rejected", cancellationToken);

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
            CurrentApproverId = leaveRequest.CurrentApproverId,
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
                CurrentApproverId = x.CurrentApproverId,
                ApprovedBy = x.ApprovedBy,
                ApprovedOn = x.ApprovedOn
            })
            .ToList()
            .AsReadOnly();
    }

    private static decimal CalculateLeaveDays(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            return 0;

        return (decimal)(endDate.Date - startDate.Date).TotalDays + 1;
    }
}