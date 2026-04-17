using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;
using Ease_HRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ease_HRM.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AppDbContext _context;

    public RolePermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<bool> MappingExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return _context.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken);
    }

    public Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return _context.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
    }

    public Task<bool> PermissionExistsAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        return _context.Permissions.AnyAsync(x => x.Id == permissionId, cancellationToken);
    }

    public Task<bool> RoleHasPermissionAsync(string roleName, string permissionName, CancellationToken cancellationToken = default)
    {
        var normalizedRole = roleName.Trim().ToLowerInvariant();
        var normalizedPermission = permissionName.Trim().ToLowerInvariant();

        return (from rolePermission in _context.RolePermissions.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on rolePermission.RoleId equals role.Id
                join permission in _context.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                where role.Name == normalizedRole && permission.Name == normalizedPermission
                select rolePermission.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await (
            from userRole in _context.UserRoles.AsNoTracking()
            join rolePermission in _context.RolePermissions.AsNoTracking()
                on userRole.RoleId equals rolePermission.RoleId
            join permission in _context.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
            select permission.Name
        )
        .Distinct()
        .ToListAsync(cancellationToken);
    }

    public Task<List<RolePermission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _context.RolePermissions
            .AsNoTracking()
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}