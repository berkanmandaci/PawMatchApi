using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Domain;

namespace PawMatch.Application.Interfaces
{
    public interface IRealtimeNotificationService
    {
        Task SendMatchNotificationAsync(int user1Id, int user2Id, MatchResultDto matchResult);
        Task SendMessageAsync(int recipientId, Message message);
    }
} 