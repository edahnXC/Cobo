using CollaborativeWorkspace.Domain.Entities;

namespace CollaborativeWorkspace.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}
