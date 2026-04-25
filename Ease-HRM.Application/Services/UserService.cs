using Ease_HRM.Application.Common.Interfaces;
using Ease_HRM.Application.DTOs.Users;
using Ease_HRM.Application.Constants;
using Ease_HRM.Application.Interfaces;
using Ease_HRM.Application.Helpers;
using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IExceptionTranslator _exceptionTranslator;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, ICurrentUserService currentUserService, IAuditLogService auditLogService, IExceptionTranslator exceptionTranslator, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _exceptionTranslator = exceptionTranslator;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = ValidationHelper.NormalizeEmail(request.Email);
        var password = ValidationHelper.RequireString(request.Password, "Password");

        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId
        };

        try
        {
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (_exceptionTranslator.IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Duplicate record detected.");
        }

        await _auditLogService.LogAsync(AuditActions.Create, AuditEntities.User, user.Id, $"User created: {user.Email}", cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(ToDto).ToList().AsReadOnly();
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            UpdatedAt = user.UpdatedAt,
            UpdatedBy = user.UpdatedBy
        };
    }
}