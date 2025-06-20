using PawMatch.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(Message message);
        Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(int user1Id, int user2Id);
        Task<Message> GetMessageByIdAsync(int messageId);
        Task UpdateMessageAsync(Message message);
        Task DeleteMessageAsync(int messageId);
    }
} 