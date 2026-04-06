using BusTicketingSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    
    public class Refund
    {
        [Key]
        public int RefundId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [Required]
        public int PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public Payment Payment { get; set; } = null!;

     
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal RefundAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CancellationFee { get; set; }

      
        [Required]
        public int RefundPercentage { get; set; }

 
        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

    
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

   
        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

      
        public DateTime? ProcessedAt { get; set; }

    
        public bool IsDeleted { get; set; } = false;
    }
}
