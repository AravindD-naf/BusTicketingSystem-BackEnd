using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.DTOs.Requests
{
    public class RouteCreateRequestDto
    {
        [Required(ErrorMessage = "Source city is required")]
        [MaxLength(150, ErrorMessage = "Source cannot exceed 150 characters")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination city is required")]
        [MaxLength(150, ErrorMessage = "Destination cannot exceed 150 characters")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Distance is required")]
        [Range(0.1, 10000, ErrorMessage = "Distance must be between 0.1 and 10000 km")]
        public decimal Distance { get; set; } // in kilometers

        [Required(ErrorMessage = "Estimated travel time is required")]
        [Range(1, 1440, ErrorMessage = "Travel time must be between 1 and 1440 minutes")]
        public int EstimatedTravelTimeMinutes { get; set; } // in minutes

        [Required(ErrorMessage = "Base fare is required")]
        [Range(0, 100000, ErrorMessage = "Base fare must be between 0 and 100000")]
        public decimal BaseFare { get; set; } // base price per seat
    }
}
