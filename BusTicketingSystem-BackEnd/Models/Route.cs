using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
    public class Route
    {
        public int RouteId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Source { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Destination { get; set; } = string.Empty;

    
        [Required]
        [Range(0.1, 10000, ErrorMessage = "Distance must be between 0.1 and 10000 km")]
        public decimal Distance { get; set; } // in kilometers


        [Required]
        [Range(1, 1440, ErrorMessage = "Travel time must be between 1 and 1440 minutes")]
        public int EstimatedTravelTimeMinutes { get; set; } // in minutes

        [Required]
        [Range(0, 100000, ErrorMessage = "Base fare must be between 0 and 100000")]
        public decimal BaseFare { get; set; } // base price per seat

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
