using BusTicketingSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

     
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

  
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

   
        [MaxLength(100)]
        public string TransactionId { get; set; } = string.Empty;

     
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

 
        [MaxLength(500)]
        public string FailureReason { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

   
        public DateTime? ProcessedAt { get; set; }


        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15);

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual Refund? Refund { get; set; }
    }
}
