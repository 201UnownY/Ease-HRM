using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ManagerExistsAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}