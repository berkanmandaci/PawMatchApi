using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PawMatch.Application.Interfaces;
using PawMatch.Application.DTOs;
using PawMatch.Domain;
using PawMatch.Api.Hubs;

namespace PawMatch.Api.Services
{
    public class SignalRNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRNotificationService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMatchNotificationAsync(int user1Id, int user2Id, MatchResultDto matchResult)
        {
            await _hubContext.Clients.User(user1Id.ToString()).SendAsync("ReceiveMatchNotification", matchResult);
            await _hubContext.Clients.User(user2Id.ToString()).SendAsync("ReceiveMatchNotification", matchResult);
        }

        public async Task SendMessageAsync(int recipientId, Message message)
        {
            await _hubContext.Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", message);
        }
    }
} 