using Ease_HRM.Application.DTOs.RolePermissions;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public RolePermissionService(IRolePermissionRepository rolePermissionRepository)
    {
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<RolePermissionDto> AssignPermissionToRoleAsync(AssignPermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RoleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId is required.");
        }

        if (request.PermissionId == Guid.Empty)
        {
            throw new ArgumentException("PermissionId is required.");
        }

        if (!await _rolePermissionRepository.RoleExistsAsync(request.RoleId, cancellationToken))
        {
            throw new InvalidOperationException("Role not found.");
        }

        if (!await _rolePermissionRepository.PermissionExistsAsync(request.PermissionId, cancellationToken))
        {
            throw new InvalidOperationException("Permission not found.");
        }

        if (await _rolePermissionRepository.MappingExistsAsync(request.RoleId, request.PermissionId, cancellationToken))
        {
            throw new InvalidOperationException("Permission is already assigned to role.");
        }

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = request.RoleId,
            PermissionId = request.PermissionId
        };

        await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        return new RolePermissionDto
        {
            Id = rolePermission.Id,
            RoleId = rolePermission.RoleId,
            PermissionId = rolePermission.PermissionId
        };
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var mappings = await _rolePermissionRepository.GetAllAsync(cancellationToken);

        return mappings
            .Select(x => new RolePermissionDto
            {
                Id = x.Id,
                RoleId = x.RoleId,
                PermissionId = x.PermissionId
            })
            .ToList()
            .AsReadOnly();
    }
}