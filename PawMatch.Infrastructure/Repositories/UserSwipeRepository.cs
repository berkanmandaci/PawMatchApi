using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace PawMatch.Infrastructure.Repositories
{
    public class UserSwipeRepository : IUserSwipeRepository
    {
        private readonly AppDbContext _context;

        public UserSwipeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserSwipe userSwipe)
        {
            await _context.UserSwipes.AddAsync(userSwipe);
            await _context.SaveChangesAsync();
        }

        public async Task<UserSwipe> GetBySwiperAndSwipedUserAsync(int swiperId, int swipedUserId)
        {
            return await _context.UserSwipes
                .FirstOrDefaultAsync(us => us.SwiperId == swiperId && us.SwipedUserId == swipedUserId);
        }
    }
} 