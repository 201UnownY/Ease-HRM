using Ease_HRM.Application.DTOs.Permissions;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ICurrentUserService _currentUserService;

    public PermissionService(IPermissionRepository permissionRepository, ICurrentUserService currentUserService)
    {
        _permissionRepository = permissionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Permission name is required.");
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();

        if (await _permissionRepository.NameExistsAsync(normalizedName, cancellationToken))
        {
            throw new InvalidOperationException("Permission name already exists.");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId
        };

        await _permissionRepository.AddAsync(permission, cancellationToken);
        await _permissionRepository.SaveChangesAsync(cancellationToken);

        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            CreatedAt = permission.CreatedAt,
            CreatedBy = permission.CreatedBy,
            UpdatedAt = permission.UpdatedAt,
            UpdatedBy = permission.UpdatedBy
        };
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

        return permissions
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToList()
            .AsReadOnly();
    }
}