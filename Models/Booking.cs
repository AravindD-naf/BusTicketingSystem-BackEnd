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

   
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual Payment? Payment { get; set; }
        public virtual Refund? Refund { get; set; }
        public virtual ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
