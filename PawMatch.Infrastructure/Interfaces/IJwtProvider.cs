using PawMatch.Domain;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IJwtProvider
    {
        string GenerateToken(User user);
    }
} 