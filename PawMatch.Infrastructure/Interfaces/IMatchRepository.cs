using System.Threading.Tasks;
using PawMatch.Domain;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IMatchRepository
    {
        Task<Match> GetByIdAsync(int matchId);
    }
} 