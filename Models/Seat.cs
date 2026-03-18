using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public class Seat
    {
        [Key]
        public int SeatId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        public Schedule Schedule { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string SeatNumber { get; set; } = string.Empty; // A1, A2, B1, etc.

        [Required]
        [MaxLength(20)]
        public string SeatStatus { get; set; } = "Available"; // Available, Locked, Booked

        public int? LockedByUserId { get; set; }

        [ForeignKey("LockedByUserId")]
        public User? LockedByUser { get; set; }

        public DateTime? LockedAt { get; set; }

        public int? BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
