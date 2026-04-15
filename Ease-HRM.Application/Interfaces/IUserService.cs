using Ease_HRM.Application.DTOs.Users;

namespace Ease_HRM.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
}