using Ease_HRM.Application.DTOs.UserRoles;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class UserRoleService : IUserRoleService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuditLogService _auditLogService;

    public UserRoleService(IUserRoleRepository userRoleRepository, IAuditLogService auditLogService)
    {
        _userRoleRepository = userRoleRepository;
        _auditLogService = auditLogService;
    }

    public async Task<UserRoleDto> AssignRoleToUserAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var userId = ValidationHelper.RequireGuid(request.UserId, "UserId");
        var roleId = ValidationHelper.RequireGuid(request.RoleId, "RoleId");

        if (!await _userRoleRepository.UserExistsAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!await _userRoleRepository.RoleExistsAsync(roleId, cancellationToken))
        {
            throw new InvalidOperationException("Role not found.");
        }

        if (await _userRoleRepository.MappingExistsAsync(userId, roleId, cancellationToken))
        {
            throw new InvalidOperationException("Role is already assigned to user.");
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId
        };

        await _userRoleRepository.AddAsync(userRole, cancellationToken);
        await _userRoleRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Assign, AuditEntities.UserRole, userRole.Id, $"Assigned role {roleId} to user {userId}", cancellationToken);

        return new UserRoleDto
        {
            Id = userRole.Id,
            UserId = userRole.UserId,
            RoleId = userRole.RoleId
        };
    }

    public async Task<IReadOnlyList<UserRoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var mappings = await _userRoleRepository.GetAllAsync(cancellationToken);

        return mappings
            .Select(x => new UserRoleDto
            {
                Id = x.Id,
                UserId = x.UserId,
                RoleId = x.RoleId
            })
            .ToList()
            .AsReadOnly();
    }
}