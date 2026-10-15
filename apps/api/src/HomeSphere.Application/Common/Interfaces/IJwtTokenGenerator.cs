using HomeSphere.Domain.Entities;

namespace HomeSphere.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(User user);
}
