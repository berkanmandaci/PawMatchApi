using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PawMatch.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            // If user is not found, we can choose to throw an exception or handle it silently
            // For now, we will assume the UserService handles the KeyNotFoundException
        }
        public async Task<List<User>> GetByIdsAsync(List<int> ids)
        {
            return await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        }
    }
} 