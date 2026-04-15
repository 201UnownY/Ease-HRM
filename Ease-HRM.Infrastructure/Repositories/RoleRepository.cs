using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.Roles.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return _context.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}