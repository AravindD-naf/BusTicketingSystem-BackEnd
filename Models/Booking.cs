using BusTicketingSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        public Schedule Schedule { get; set; } = null!;

        [Required]
        public int NumberOfSeats { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

  
        [Required]
        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;

   
        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

  
        public DateTime? LastStatusChangeAt { get; set; }

        [MaxLength(500)]
        public string CancellationReason { get; set; } = string.Empty;

        [MaxLength(20)]
        public string CancelledBy { get; set; } = string.Empty;

        // Unique PNR for this booking — generated on creation
        [MaxLength(20)]
        public string PNR { get; set; } = string.Empty;

        // Promo code applied to this booking
        [MaxLength(50)]
        public string? PromoCodeUsed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        // Boarding & drop points
        [MaxLength(200)]
        public string? BoardingPointName { get; set; }

        [MaxLength(200)]
        public string? DropPointName { get; set; }

        // Contact details
        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual Payment? Payment { get; set; }
        public virtual Refund? Refund { get; set; }
        public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public virtual BusRating? BusRating { get; set; }
    }
}
