using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsReadByReceiver { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}
