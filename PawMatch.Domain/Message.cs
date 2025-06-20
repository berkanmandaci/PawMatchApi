using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PawMatch.Domain
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        [ForeignKey("RecipientId")]
        public User Recipient { get; set; }
    }
} 