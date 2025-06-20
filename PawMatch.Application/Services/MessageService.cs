using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PawMatch.Application.Interfaces;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;

namespace PawMatch.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly IUserRepository _userRepository;

        public MessageService(IMessageRepository messageRepository, IRealtimeNotificationService realtimeNotificationService, IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _realtimeNotificationService = realtimeNotificationService;
            _userRepository = userRepository;
        }

        public async Task<Message> SendMessageAsync(int senderId, int recipientId, string content)
        {
            // Kullanıcı kontrolü
            var sender = await _userRepository.GetByIdAsync(senderId);
            if (sender == null)
                throw new ArgumentException($"Sender user not found: {senderId}");
            var recipient = await _userRepository.GetByIdAsync(recipientId);
            if (recipient == null)
                throw new ArgumentException($"Recipient user not found: {recipientId}");

            // (Opsiyonel) Yetkilendirme kontrolü: senderId ile dışarıdan gelen currentUserId eşleşmiyorsa
            // if (currentUserId != senderId) throw new UnauthorizedAccessException();

            var message = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _messageRepository.AddMessageAsync(message);

            // Mesajı alıcıya SignalR üzerinden gönder
            await _realtimeNotificationService.SendMessageAsync(recipientId, message);

            return message;
        }

        public async Task<IEnumerable<Message>> GetChatHistoryAsync(int user1Id, int user2Id)
        {
            return await _messageRepository.GetMessagesBetweenUsersAsync(user1Id, user2Id);
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId)
        {
            var message = await _messageRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            message.IsRead = true;
            await _messageRepository.UpdateMessageAsync(message);
            return true;
        }
    }
} 