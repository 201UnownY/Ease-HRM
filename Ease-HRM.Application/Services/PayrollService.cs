using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.Payroll;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IWorkScheduleRepository _workScheduleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IExceptionTranslator _exceptionTranslator;

    public PayrollService(IPayrollRepository payrollRepository, IWorkScheduleRepository workScheduleRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService, IExceptionTranslator exceptionTranslator)
    {
        _payrollRepository = payrollRepository;
        _workScheduleRepository = workScheduleRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _exceptionTranslator = exceptionTranslator;
    }

    public async Task<SalaryStructureDto> CreateSalaryStructureAsync(CreateSalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        ValidationHelper.RequireGuid(request.EmployeeId, "EmployeeId");
        ValidationHelper.EnsurePositive(request.BaseSalary, "BaseSalary");
        ValidationHelper.EnsureNonNegative(request.HRA, "HRA");
        ValidationHelper.EnsureNonNegative(request.Allowances, "Allowances");
        ValidationHelper.EnsureNonNegative(request.Deductions, "Deductions");

        var employeeExists = await _payrollRepository.EmployeeExistsAsync(request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var effectiveFrom = DateTime.UtcNow.Date;
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var changeReason = "Initial salary structure created";
        var now = DateTime.UtcNow;

        var salaryStructure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            BaseSalary = request.BaseSalary,
            HRA = request.HRA,
            Allowances = request.Allowances,
            Deductions = request.Deductions,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = changeReason
        };

        await _payrollRepository.ExecuteInTransactionAsync(async ct =>
        {
            var hasOverlap = await _payrollRepository.HasOverlappingSalaryStructureAsync(
                request.EmployeeId,
                effectiveFrom,
                null,
                null,
                ct);

            if (hasOverlap)
            {
                throw new InvalidOperationException("Overlapping salary structure exists for this employee.");
            }

            await _payrollRepository.AddSalaryStructureAsync(salaryStructure, ct);
            await _payrollRepository.SaveChangesAsync(ct);
            await _auditLogService.LogAsync(
                AuditActions.Create,
                AuditEntities.SalaryStructure,
                salaryStructure.Id,
                $"Salary structure created for employee {request.EmployeeId}",
                ct);
        }, cancellationToken);

        return new SalaryStructureDto
        {
            Id = salaryStructure.Id,
            EmployeeId = salaryStructure.EmployeeId,
            BaseSalary = salaryStructure.BaseSalary,
            HRA = salaryStructure.HRA,
            Allowances = salaryStructure.Allowances,
            Deductions = salaryStructure.Deductions,
            EffectiveFrom = salaryStructure.EffectiveFrom,
            EffectiveTo = salaryStructure.EffectiveTo
        };
    }

    public async Task<SalaryStructureDto> UpdateSalaryStructureAsync(UpdateSalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        var salaryStructureId = ValidationHelper.RequireGuid(request.SalaryStructureId, nameof(request.SalaryStructureId));
        ValidationHelper.EnsurePositive(request.BaseSalary, nameof(request.BaseSalary));
        ValidationHelper.EnsureNonNegative(request.HRA, nameof(request.HRA));
        ValidationHelper.EnsureNonNegative(request.Allowances, nameof(request.Allowances));
        ValidationHelper.EnsureNonNegative(request.Deductions, nameof(request.Deductions));

        var existing = await _payrollRepository.GetSalaryStructureByIdAsync(salaryStructureId, cancellationToken)
            ?? throw new InvalidOperationException("Salary structure not found.");

        var effectiveFrom = request.EffectiveFrom.Date;

        if (effectiveFrom <= existing.EffectiveFrom)
        {
            throw new InvalidOperationException("New effective date must be after current version.");
        }

        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var now = DateTime.UtcNow;
        var reason = string.IsNullOrWhiteSpace(request.ChangeReason) ? "Salary structure updated" : request.ChangeReason.Trim();

        var newVersion = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            EmployeeId = existing.EmployeeId,
            BaseSalary = request.BaseSalary,
            HRA = request.HRA,
            Allowances = request.Allowances,
            Deductions = request.Deductions,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorId,
            UpdatedBy = actorId,
            ChangeReason = reason
        };

        await _payrollRepository.ExecuteInTransactionAsync(async ct =>
        {
            var overlap = await _payrollRepository.HasOverlappingSalaryStructureAsync(
                existing.EmployeeId,
                effectiveFrom,
                null,
                salaryStructureId,
                ct);

            if (overlap)
            {
                throw new InvalidOperationException("Overlapping salary structure exists for this employee.");
            }

            existing.Supersede(effectiveFrom, actorId);
            existing.ValidateVersioning();

            await _payrollRepository.SaveChangesAsync(ct);
            await _payrollRepository.AddSalaryStructureAsync(newVersion, ct);
            await _payrollRepository.SaveChangesAsync(ct);

            var details = $"SalaryStructure updated (OldId: {existing.Id} → NewId: {newVersion.Id}, Reason: {reason})";
            await _auditLogService.LogAsync(AuditActions.Update, AuditEntities.SalaryStructure, newVersion.Id, details, ct);
        }, cancellationToken);

        return new SalaryStructureDto
        {
            Id = newVersion.Id,
            EmployeeId = newVersion.EmployeeId,
            BaseSalary = newVersion.BaseSalary,
            HRA = newVersion.HRA,
            Allowances = newVersion.Allowances,
            Deductions = newVersion.Deductions,
            EffectiveFrom = newVersion.EffectiveFrom,
            EffectiveTo = newVersion.EffectiveTo
        };
    }

    public async Task<PayrollDto> GeneratePayrollAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        ValidationHelper.RequireGuid(employeeId, "EmployeeId");
        ValidationHelper.RequireValidYear(year);
        ValidationHelper.RequireValidMonth(month);

        if (year == DateTime.UtcNow.Year && month > DateTime.UtcNow.Month)
        {
            throw new ArgumentException("Cannot generate payroll for future month.");
        }

        var payrollDate = new DateTime(year, month, 1);

        var salaryStructure = await _payrollRepository.GetEffectiveSalaryAsync(employeeId, payrollDate, cancellationToken);
        if (salaryStructure is null)
        {
            throw new InvalidOperationException("Salary structure not found for employee.");
        }

        var attendanceSessions = await _payrollRepository.GetAttendanceAsync(employeeId, year, month, cancellationToken);
        var leaveRequests = await _payrollRepository.GetLeavesAsync(employeeId, year, month, cancellationToken);

        var policy = await _payrollRepository.GetEffectiveAttendancePolicyAsync(new DateTime(year, month, 1), cancellationToken);
        if (policy is null)
        {
            throw new InvalidOperationException("Attendance policy not configured.");
        }

        var workingDayWeights = await _workScheduleRepository.GetWorkingDateWeights(employeeId, year, month, cancellationToken);
        var workingDays = workingDayWeights.ToList();

        if (!workingDays.Any(x => x.Value > 0))
        {
            throw new InvalidOperationException("Work schedule not configured for the selected period.");
        }

        var gross = salaryStructure.BaseSalary + salaryStructure.HRA + salaryStructure.Allowances;
        var totalWorkingDayUnits = workingDays.Sum(x => x.Value);
        if (totalWorkingDayUnits <= 0)
        {
            throw new InvalidOperationException("Invalid working day units in work schedule.");
        }

        var perDaySalary = Math.Round(gross / totalWorkingDayUnits, 4);

        var attendanceDateMap = new Dictionary<DateTime, AttendanceStatus>();
        var sessionsGroupedByDate = attendanceSessions.GroupBy(x => x.Date);

        foreach (var group in sessionsGroupedByDate)
        {
            var totalHours = AttendanceCalculator.CalculateTotalHours(group);

            AttendanceStatus status = AttendanceStatus.Absent;
            if (totalHours >= policy.FullDayHours)
                status = AttendanceStatus.Present;
            else if (totalHours >= policy.HalfDayHours)
                status = AttendanceStatus.HalfDay;

            attendanceDateMap[group.Key] = status;
        }

        var leaveTypeIds = leaveRequests.Select(x => x.LeaveTypeId).Distinct().ToList();
        var leaveTypes = await _payrollRepository.GetLeaveTypesAsync(leaveTypeIds, cancellationToken);
        if (leaveTypes.Count != leaveTypeIds.Count)
        {
            throw new InvalidOperationException("Invalid leave type configuration.");
        }

        var leaveTypeMetadata = leaveTypes.ToDictionary(
            x => x.Id,
            x => new LeaveTypeMetadata(Math.Clamp(x.Weight, 0m, 1m), x.IsPaid));

        var (leaveDeduction, attendanceDeduction) = CalculateDeductions(
            perDaySalary,
            workingDayWeights,
            leaveRequests,
            leaveTypeMetadata,
            attendanceDateMap);

        var totalDeduction = attendanceDeduction + leaveDeduction + salaryStructure.Deductions;
        var netSalary = Math.Max(0, gross - totalDeduction);
        var roundedNetSalary = Math.Round(netSalary, 2, MidpointRounding.AwayFromZero);

        var payroll = new Payroll
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Year = year,
            Month = month,
            BaseSalary = salaryStructure.BaseSalary,
            HRA = salaryStructure.HRA,
            Allowances = salaryStructure.Allowances,
            AttendanceDeduction = attendanceDeduction,
            LeaveDeduction = leaveDeduction,
            NetSalary = roundedNetSalary,
            GeneratedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        try
        {
            await _payrollRepository.ExecuteInTransactionAsync(async ct =>
            {
                await _payrollRepository.AddPayrollAsync(payroll, ct);
                await _payrollRepository.SaveChangesAsync(ct);
                await _auditLogService.LogAsync(AuditActions.Generate, AuditEntities.Payroll, payroll.Id, $"Payroll generated for employee {employeeId} ({month}/{year})", ct);
            }, cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsUniqueConstraintViolation(ex))
        {
            var existing = await _payrollRepository.GetPayrollAsync(employeeId, year, month, cancellationToken);
            if (existing != null)
            {
                return new PayrollDto
                {
                    Id = existing.Id,
                    EmployeeId = existing.EmployeeId,
                    Year = existing.Year,
                    Month = existing.Month,
                    BaseSalary = existing.BaseSalary,
                    HRA = existing.HRA,
                    Allowances = existing.Allowances,
                    LeaveDeduction = Math.Round(existing.LeaveDeduction, 2, MidpointRounding.AwayFromZero),
                    AttendanceDeduction = Math.Round(existing.AttendanceDeduction, 2, MidpointRounding.AwayFromZero),
                    NetSalary = Math.Round(existing.NetSalary, 2, MidpointRounding.AwayFromZero),
                    GeneratedAt = existing.GeneratedAt
                };
            }

            throw;
        }

        return new PayrollDto
        {
            Id = payroll.Id,
            EmployeeId = payroll.EmployeeId,
            Year = payroll.Year,
            Month = payroll.Month,
            BaseSalary = payroll.BaseSalary,
            HRA = payroll.HRA,
            Allowances = payroll.Allowances,
            LeaveDeduction = Math.Round(payroll.LeaveDeduction, 2, MidpointRounding.AwayFromZero),
            AttendanceDeduction = Math.Round(payroll.AttendanceDeduction, 2, MidpointRounding.AwayFromZero),
            NetSalary = Math.Round(payroll.NetSalary, 2, MidpointRounding.AwayFromZero),
            GeneratedAt = payroll.GeneratedAt
        };
    }

    public async Task<IReadOnlyList<PayrollDto>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        ValidationHelper.RequireGuid(employeeId, "EmployeeId");

        var payrolls = await _payrollRepository.GetPayrollsAsync(employeeId, cancellationToken);

        return payrolls
            .Select(x => new PayrollDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                Year = x.Year,
                Month = x.Month,
                BaseSalary = x.BaseSalary,
                HRA = x.HRA,
                Allowances = x.Allowances,
                LeaveDeduction = Math.Round(x.LeaveDeduction, 2, MidpointRounding.AwayFromZero),
                AttendanceDeduction = Math.Round(x.AttendanceDeduction, 2, MidpointRounding.AwayFromZero),
                NetSalary = Math.Round(x.NetSalary, 2, MidpointRounding.AwayFromZero),
                GeneratedAt = x.GeneratedAt
            })
            .ToList()
            .AsReadOnly();
    }

    private static (decimal LeaveDeduction, decimal AttendanceDeduction) CalculateDeductions(
        decimal perDaySalary,
        IReadOnlyDictionary<DateTime, decimal> workingDayWeights,
        List<LeaveRequest> leaveRequests,
        IReadOnlyDictionary<Guid, LeaveTypeMetadata> leaveTypeMetadata,
        IReadOnlyDictionary<DateTime, AttendanceStatus> attendanceDateMap)
    {
        var leaveLookup = new LeaveRangeLookup(leaveRequests, leaveTypeMetadata);
        decimal leaveDeduction = 0;
        decimal attendanceDeduction = 0;

        foreach (var workingDay in workingDayWeights)
        {
            var currentDate = workingDay.Key;
            var dayWeight = workingDay.Value;

            decimal leaveCoverage = 0m;
            bool leaveIsPaid = true;

            if (leaveLookup.TryGetLeave(currentDate, out var activeLeave))
            {
                leaveCoverage = activeLeave.Weight;
                leaveIsPaid = activeLeave.IsPaid;
            }

            if (!leaveIsPaid && leaveCoverage > 0m)
            {
                leaveDeduction += perDaySalary * dayWeight * leaveCoverage;
            }

            var remainingCoverage = Math.Clamp(1m - leaveCoverage, 0m, 1m);
            if (remainingCoverage <= 0m)
            {
                continue;
            }

            var attendanceFactor = GetAttendanceFactor(attendanceDateMap, currentDate);
            attendanceDeduction += perDaySalary * dayWeight * remainingCoverage * attendanceFactor;
        }

        return (leaveDeduction, attendanceDeduction);
    }

    private static decimal GetAttendanceFactor(IReadOnlyDictionary<DateTime, AttendanceStatus> attendanceDateMap, DateTime date)
    {
        if (!attendanceDateMap.TryGetValue(date, out var status))
        {
            // No attendance record → treated as Absent (full deduction)
            return 1m;
        }

        return status switch
        {
            AttendanceStatus.Absent => 1m,
            AttendanceStatus.HalfDay => 0.5m,
            _ => 0m
        };
    }

    private readonly record struct LeaveTypeMetadata(decimal Weight, bool IsPaid);

    private readonly record struct ActiveLeave(Guid LeaveTypeId, decimal Weight, bool IsPaid, DateTime End);

    private sealed class LeaveRangeLookup
    {
        private readonly List<(DateTime Start, DateTime End, Guid LeaveTypeId, decimal Weight, bool IsPaid)> _sorted;
        private readonly PriorityQueue<ActiveLeave, (int UnpaidPriority, decimal WeightPriority, DateTime EndDate, Guid LeaveTypeId)> _active;
        private int _nextIndex;

        public LeaveRangeLookup(IEnumerable<LeaveRequest> leaveRequests, IReadOnlyDictionary<Guid, LeaveTypeMetadata> leaveTypeMetadata)
        {
            _sorted = leaveRequests
                .Select(x =>
                {
                    if (!leaveTypeMetadata.TryGetValue(x.LeaveTypeId, out var metadata))
                    {
                        throw new InvalidOperationException("Leave type metadata missing.");
                    }

                    return (Start: x.StartDate.Date, End: x.EndDate.Date, x.LeaveTypeId, metadata.Weight, metadata.IsPaid);
                })
                .OrderBy(x => x.Start)
                .ToList();

            _active = new PriorityQueue<ActiveLeave, (int UnpaidPriority, decimal WeightPriority, DateTime EndDate, Guid LeaveTypeId)>();
            _nextIndex = 0;
        }

        public bool TryGetLeave(DateTime date, out ActiveLeave leave)
        {
            var targetDate = date.Date;

            while (_nextIndex < _sorted.Count && _sorted[_nextIndex].Start <= targetDate)
            {
                var range = _sorted[_nextIndex];
                var activeLeave = new ActiveLeave(range.LeaveTypeId, range.Weight, range.IsPaid, range.End);
                var priority = (
                    UnpaidPriority: range.IsPaid ? 1 : 0,
                    WeightPriority: -range.Weight,
                    EndDate: range.End,
                    LeaveTypeId: range.LeaveTypeId);

                _active.Enqueue(activeLeave, priority);
                _nextIndex++;
            }

            while (_active.Count > 0)
            {
                var top = _active.Peek();
                if (top.End >= targetDate)
                {
                    break;
                }

                _active.Dequeue();
            }

            if (_active.Count == 0)
            {
                leave = default;
                return false;
            }

            leave = _active.Peek();
            return true;
        }
    }
}