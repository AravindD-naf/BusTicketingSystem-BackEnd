using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
    public class Bus
    {
        public int BusId { get; set; }

        [Required]
        [MaxLength(20)]
        public string BusNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BusType { get; set; } = string.Empty; // AC / Non-AC / Sleeper

        [Required]
        [Range(1, 40, ErrorMessage = "Total seats must be between 1 and 40")]
        public int TotalSeats { get; set; }

        [Required]
        [MaxLength(100)]
        public string OperatorName { get; set; } = string.Empty;

        public double RatingAverage { get; set; } = 0;

        public bool IsActive { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}