using Ease_HRM.Application.DTOs.Roles;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;

    public RoleService(IRoleRepository roleRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService)
    {
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = StringHelper.Normalize(request.Name, "Role name");

        if (await _roleRepository.NameExistsAsync(normalizedName, cancellationToken))
        {
            throw new InvalidOperationException("Role name already exists.");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.Role, role.Id, $"Role created: {role.Name}", cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            UpdatedAt = role.UpdatedAt,
            UpdatedBy = role.UpdatedBy
        };
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);

        return roles
            .Select(x => new RoleDto
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToList()
            .AsReadOnly();
    }
}