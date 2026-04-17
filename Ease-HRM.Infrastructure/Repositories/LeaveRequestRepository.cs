using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests
            .AsNoTracking()
            .OrderByDescending(x => x.AppliedOn)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken);
    }

    public Task<Employee?> GetEmployeeByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<Employee>> GetHierarchyEmployeesAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Select(x => new Employee
            {
                Id = x.Id,
                ManagerId = x.ManagerId
            })
            .ToListAsync(cancellationToken);

        var employeeMap = employees.ToDictionary(x => x.Id, x => x);

        if (!employeeMap.ContainsKey(employeeId))
        {
            return new List<Employee>();
        }

        var result = new List<Employee>();
        var currentId = employeeId;
        var visited = new HashSet<Guid>();

        while (employeeMap.TryGetValue(currentId, out var current) && visited.Add(currentId))
        {
            result.Add(current);

            if (!current.ManagerId.HasValue)
            {
                break;
            }

            currentId = current.ManagerId.Value;
        }

        return result;
    }

    public Task<bool> LeaveTypeExistsAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return _context.LeaveTypes.AnyAsync(x => x.Id == leaveTypeId, cancellationToken);
    }

    public Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default)
    {
        return _context.LeaveBalances
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year, cancellationToken);
    }

    public Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequests.AnyAsync(x =>
            x.EmployeeId == employeeId &&
            !x.IsDeleted &&
            x.Status != Domain.Enums.LeaveStatus.Rejected &&
            x.StartDate <= end &&
            x.EndDate >= start,
            cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}