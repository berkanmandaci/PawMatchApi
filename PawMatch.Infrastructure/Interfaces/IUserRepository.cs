using System.Threading.Tasks;
using PawMatch.Domain;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByIdAsync(int id);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<List<User>> GetByIdsAsync(List<int> ids);
    }
} 