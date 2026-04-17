using Ease_HRM.Application.DTOs.RolePermissions;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IAuditLogService _auditLogService;

    public RolePermissionService(IRolePermissionRepository rolePermissionRepository, IAuditLogService auditLogService)
    {
        _rolePermissionRepository = rolePermissionRepository;
        _auditLogService = auditLogService;
    }

    public async Task<RolePermissionDto> AssignPermissionToRoleAsync(AssignPermissionRequest request, CancellationToken cancellationToken = default)
    {
        var roleId = ValidationHelper.RequireGuid(request.RoleId, "RoleId");
        var permissionId = ValidationHelper.RequireGuid(request.PermissionId, "PermissionId");

        if (!await _rolePermissionRepository.RoleExistsAsync(roleId, cancellationToken))
        {
            throw new InvalidOperationException("Role not found.");
        }

        if (!await _rolePermissionRepository.PermissionExistsAsync(permissionId, cancellationToken))
        {
            throw new InvalidOperationException("Permission not found.");
        }

        if (await _rolePermissionRepository.MappingExistsAsync(roleId, permissionId, cancellationToken))
        {
            throw new InvalidOperationException("Permission is already assigned to role.");
        }

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId
        };

        await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);
        await _rolePermissionRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Assign, AuditEntities.RolePermission, rolePermission.Id, $"Assigned permission {permissionId} to role {roleId}", cancellationToken);

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