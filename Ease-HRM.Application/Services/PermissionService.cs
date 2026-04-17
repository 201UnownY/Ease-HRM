using Ease_HRM.Application.DTOs.Permissions;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Ease_HRM.Application.Services;

public class PermissionService : IPermissionService
{
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(5);
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMemoryCache _memoryCache;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        ICurrentUserService currentUserService,
        IMemoryCache memoryCache)
    {
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _currentUserService = currentUserService;
        _memoryCache = memoryCache;
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = StringHelper.Normalize(request.Name, "Permission name");

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

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"permissions:{userId}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyCollection<string>? cached) && cached is not null)
        {
            return cached;
        }

        var permissions = await _rolePermissionRepository.GetUserPermissionsAsync(userId, cancellationToken);

        var normalized = permissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        _memoryCache.Set(cacheKey, normalized, PermissionCacheDuration);
        return normalized;
    }
}