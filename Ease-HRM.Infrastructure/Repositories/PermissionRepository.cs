using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _context;

    public PermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.Permissions.AnyAsync(x => x.Name == name, cancellationToken);
    }

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(permission, cancellationToken);
    }

    public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return _context.Permissions.AnyAsync(x => x.Id == permissionId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}