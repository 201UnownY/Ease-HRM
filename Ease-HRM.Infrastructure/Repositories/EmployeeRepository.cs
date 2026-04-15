using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> ManagerExistsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return _context.Employees.AnyAsync(x => x.Id == managerId, cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }

    public Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Employees
            .AsNoTracking()
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}