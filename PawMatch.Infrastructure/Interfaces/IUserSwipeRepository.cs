using PawMatch.Domain;
using System.Threading.Tasks;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IUserSwipeRepository
    {
        Task AddAsync(UserSwipe userSwipe);
        Task<UserSwipe> GetBySwiperAndSwipedUserAsync(int swiperId, int swipedUserId);
        // You can add more methods here as needed, e.g., to get recent swipes
    }
} 