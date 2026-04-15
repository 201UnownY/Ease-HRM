using Ease_HRM.Application.DTOs.Payroll;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Domain.Enums;

namespace Ease_HRM.Application.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;

    public PayrollService(IPayrollRepository payrollRepository)
    {
        _payrollRepository = payrollRepository;
    }

    public async Task<SalaryStructureDto> CreateSalaryStructureAsync(CreateSalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.");
        }

        if (request.BaseSalary <= 0)
        {
            throw new ArgumentException("BaseSalary must be greater than 0.");
        }

        if (request.HRA < 0 || request.Allowances < 0 || request.Deductions < 0)
        {
            throw new ArgumentException("HRA, Allowances, and Deductions cannot be negative.");
        }

        var employeeExists = await _payrollRepository.EmployeeExistsAsync(request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        await _payrollRepository.DeactivateSalaryStructuresAsync(request.EmployeeId, cancellationToken);

        var now = DateTime.UtcNow;

        var salaryStructure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            BaseSalary = request.BaseSalary,
            HRA = request.HRA,
            Allowances = request.Allowances,
            Deductions = request.Deductions,
            EffectiveFrom = now,
            IsActive = true
        };

        await _payrollRepository.AddSalaryStructureAsync(salaryStructure, cancellationToken);
        await _payrollRepository.SaveChangesAsync(cancellationToken);

        return new SalaryStructureDto
        {
            Id = salaryStructure.Id,
            EmployeeId = salaryStructure.EmployeeId,
            BaseSalary = salaryStructure.BaseSalary,
            HRA = salaryStructure.HRA,
            Allowances = salaryStructure.Allowances,
            Deductions = salaryStructure.Deductions,
            EffectiveFrom = salaryStructure.EffectiveFrom,
            IsActive = salaryStructure.IsActive
        };
    }

    public async Task<PayrollDto> GeneratePayrollAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.");
        }

        if (year < 2000 || year > DateTime.UtcNow.Year)
        {
            throw new ArgumentException("Invalid year.");
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.");
        }

        if (year == DateTime.UtcNow.Year && month > DateTime.UtcNow.Month)
        {
            throw new ArgumentException("Cannot generate payroll for future month.");
        }

        var payrollExists = await _payrollRepository.PayrollExistsAsync(employeeId, year, month, cancellationToken);
        if (payrollExists)
        {
            throw new InvalidOperationException("Payroll already generated for this employee in the specified month.");
        }

        var salaryStructure = await _payrollRepository.GetActiveSalaryAsync(employeeId, cancellationToken);
        if (salaryStructure is null)
        {
            throw new InvalidOperationException("Salary structure not found for employee.");
        }

        var attendanceRecords = await _payrollRepository.GetAttendanceAsync(employeeId, year, month, cancellationToken);
        var leaveRequests = await _payrollRepository.GetLeavesAsync(employeeId, year, month, cancellationToken);

        var gross = salaryStructure.BaseSalary + salaryStructure.HRA + salaryStructure.Allowances;
        var daysInMonth = GetDaysInMonth(year, month);
        var perDaySalary = Math.Round(gross / daysInMonth, 2);

        // Build leave date to leave type mapping
        var leaveDateMap = BuildLeaveDateMap(leaveRequests);

        // Build attendance date to status mapping for O(1) lookup
        var attendanceDateMap = attendanceRecords.ToDictionary(x => x.Date, x => x.Status);

        // Get unpaid leave type IDs
        var leaveTypeIds = leaveRequests.Select(x => x.LeaveTypeId).Distinct().ToList();
        var leaveTypes = await _payrollRepository.GetLeaveTypesAsync(leaveTypeIds, cancellationToken);

        var unpaidLeaveTypeIds = leaveTypes
            .Where(x => !x.IsPaid)
            .Select(x => x.Id)
            .ToHashSet();

        decimal attendanceDeduction = 0;
        decimal leaveDeduction = 0;

        // Process all days of the month
        var firstDayOfMonth = new DateTime(year, month, 1);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = firstDayOfMonth.AddDays(day - 1);

            if (leaveDateMap.TryGetValue(currentDate, out var leaveTypeId))
            {
                // Leave exists on this date
                if (unpaidLeaveTypeIds.Contains(leaveTypeId))
                {
                    leaveDeduction += perDaySalary;
                }
            }
            else
            {
                // No leave, process attendance
                if (attendanceDateMap.TryGetValue(currentDate, out var attendanceStatus))
                {
                    // Attendance record exists
                    if (attendanceStatus == AttendanceStatus.Absent)
                    {
                        attendanceDeduction += perDaySalary;
                    }
                    else if (attendanceStatus == AttendanceStatus.HalfDay)
                    {
                        attendanceDeduction += Math.Round(perDaySalary / 2, 2);
                    }
                }
                else
                {
                    // No attendance record, treat as Absent
                    attendanceDeduction += perDaySalary;
                }
            }
        }

        var totalDeduction = attendanceDeduction + leaveDeduction + salaryStructure.Deductions;
        var netSalary = Math.Max(0, gross - totalDeduction);

        var payroll = new Payroll
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Year = year,
            Month = month,
            BaseSalary = salaryStructure.BaseSalary,
            HRA = salaryStructure.HRA,
            Allowances = salaryStructure.Allowances,
            AttendanceDeduction = Math.Round(attendanceDeduction, 2),
            LeaveDeduction = Math.Round(leaveDeduction, 2),
            NetSalary = Math.Round(netSalary, 2),
            GeneratedAt = DateTime.UtcNow
        };

        await _payrollRepository.AddPayrollAsync(payroll, cancellationToken);
        await _payrollRepository.SaveChangesAsync(cancellationToken);

        return new PayrollDto
        {
            Id = payroll.Id,
            EmployeeId = payroll.EmployeeId,
            Year = payroll.Year,
            Month = payroll.Month,
            BaseSalary = payroll.BaseSalary,
            HRA = payroll.HRA,
            Allowances = payroll.Allowances,
            LeaveDeduction = payroll.LeaveDeduction,
            AttendanceDeduction = payroll.AttendanceDeduction,
            NetSalary = payroll.NetSalary,
            GeneratedAt = payroll.GeneratedAt
        };
    }

    public async Task<IReadOnlyList<PayrollDto>> GetPayrollsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.");
        }

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
                LeaveDeduction = x.LeaveDeduction,
                AttendanceDeduction = x.AttendanceDeduction,
                NetSalary = x.NetSalary,
                GeneratedAt = x.GeneratedAt
            })
            .ToList()
            .AsReadOnly();
    }

    private static int GetDaysInMonth(int year, int month)
    {
        return DateTime.DaysInMonth(year, month);
    }

    private static Dictionary<DateTime, Guid> BuildLeaveDateMap(List<LeaveRequest> leaveRequests)
    {
        var leaveDateMap = new Dictionary<DateTime, Guid>();

        foreach (var leave in leaveRequests)
        {
            var currentDate = leave.StartDate.Date;
            var endDate = leave.EndDate.Date;

            while (currentDate <= endDate)
            {
                if (!leaveDateMap.ContainsKey(currentDate))
                {
                    leaveDateMap[currentDate] = leave.LeaveTypeId;
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        return leaveDateMap;
    }
}