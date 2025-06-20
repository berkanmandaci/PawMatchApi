using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PawMatch.Application.Interfaces;
using PawMatch.Application.DTOs;
using PawMatch.Domain;

namespace PawMatch.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Kullanıcı ID'lerini bağlantı ID'leriyle ilişkilendirmek için bir mekanizma
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new ConcurrentDictionary<string, string>();
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;

        public ChatHub(IMessageService messageService, IUserService userService)
        {
            _messageService = messageService;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                _connectedUsers.TryAdd(userId, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                _connectedUsers.TryRemove(userId, out _);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // SignalR ile doğrudan mesaj gönderme fonksiyonu
        public async Task SendMessageToUser(int recipientUserId, string content)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId))
                throw new HubException("Kullanıcı kimliği doğrulanamadı.");

            if (senderId == recipientUserId)
                throw new HubException("Kendinize mesaj gönderemezsiniz.");

            // Kullanıcılar var mı kontrolü
            var sender = await _userService.GetUserByIdAsync(senderId);
            var recipient = await _userService.GetUserByIdAsync(recipientUserId);
            if (sender == null || recipient == null)
                throw new HubException("Kullanıcı(lar) bulunamadı.");

            // Mesajı kaydet
            var message = await _messageService.SendMessageAsync(senderId, recipientUserId, content);

            // DTO mapping
            var senderDto = UserPublicDtoMapper.ToPublicDto(sender, sender.GetPhotoIds(), sender.GetPetIds());
            var messageDto = MessageDtoMapper.ToDto(message, senderDto);

            // Hem gönderen hem alıcıya ilet
            await Clients.User(senderId.ToString()).SendAsync("ReceiveMessage", messageDto);
            await Clients.User(recipientUserId.ToString()).SendAsync("ReceiveMessage", messageDto);
        }

        // İleride mesaj gönderme ve alma metodları buraya eklenebilir.
        // Örneğin: public async Task SendMessageToUser(string recipientUserId, string message) { ... }
    }
} 