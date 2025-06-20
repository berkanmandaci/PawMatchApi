using System.Collections.Generic;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Domain;

namespace PawMatch.Application.Interfaces
{
    public interface IMessageService
    {
        Task<Message> SendMessageAsync(int senderId, int recipientId, string content);
        Task<IEnumerable<Message>> GetChatHistoryAsync(int user1Id, int user2Id);
        Task<bool> MarkMessageAsReadAsync(int messageId);
    }
} 