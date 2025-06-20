using Microsoft.EntityFrameworkCore;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;

namespace PawMatch.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _db;

        public MessageRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddMessageAsync(Message message)
        {
            await _db.Messages.AddAsync(message);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(int user1Id, int user2Id)
        {
            return await _db.Messages
                .Where(m => (m.SenderId == user1Id && m.RecipientId == user2Id) ||
                            (m.SenderId == user2Id && m.RecipientId == user1Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<Message> GetMessageByIdAsync(int messageId)
        {
            return await _db.Messages.FindAsync(messageId);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _db.Messages.Update(message);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteMessageAsync(int messageId)
        {
            var message = await _db.Messages.FindAsync(messageId);
            if (message != null)
            {
                _db.Messages.Remove(message);
                await _db.SaveChangesAsync();
            }
        }
    }
} 