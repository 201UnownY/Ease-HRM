using Ease_HRM.Domain.Entities;

namespace Ease_HRM.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, IEnumerable<string> permissions);
    DateTime GetExpirationUtc();
}