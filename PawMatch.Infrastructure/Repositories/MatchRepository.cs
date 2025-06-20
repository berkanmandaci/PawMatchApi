using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;

namespace PawMatch.Infrastructure.Repositories
{
    public class MatchRepository : IMatchRepository
    {
        private readonly AppDbContext _db;
        public MatchRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Match> GetByIdAsync(int matchId)
        {
            return await _db.Matches
                .Include(m => m.User1)
                .Include(m => m.User2)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        }
    }
} 