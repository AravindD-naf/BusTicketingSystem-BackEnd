using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public class BusRating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [Required]
        public int BusId { get; set; }

        [ForeignKey("BusId")]
        public Bus Bus { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
