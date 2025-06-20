using System;
using PawMatch.Domain;

namespace PawMatch.Application.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public UserPublicDto Sender { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

    public static class MessageDtoMapper
    {
        public static MessageDto ToDto(Message message, UserPublicDto sender)
        {
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Sender = sender,
                Content = message.Content,
                Timestamp = message.Timestamp,
                IsRead = message.IsRead
            };
        }
    }
} 