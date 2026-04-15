using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly AppDbContext _context;

    public UserRoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return _context.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
    }

    public Task<bool> MappingExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return _context.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);
    }

    public Task<List<UserRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.UserRoles
            .AsNoTracking()
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.RoleId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _context.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}