using Ease_HRM.Application.DTOs.UserRoles;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class UserRoleService : IUserRoleService
{
    private readonly IUserRoleRepository _userRoleRepository;

    public UserRoleService(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<UserRoleDto> AssignRoleToUserAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (request.RoleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId is required.");
        }

        if (!await _userRoleRepository.UserExistsAsync(request.UserId, cancellationToken))
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!await _userRoleRepository.RoleExistsAsync(request.RoleId, cancellationToken))
        {
            throw new InvalidOperationException("Role not found.");
        }

        if (await _userRoleRepository.MappingExistsAsync(request.UserId, request.RoleId, cancellationToken))
        {
            throw new InvalidOperationException("Role is already assigned to user.");
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            RoleId = request.RoleId
        };

        await _userRoleRepository.AddAsync(userRole, cancellationToken);
        await _userRoleRepository.SaveChangesAsync(cancellationToken);

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