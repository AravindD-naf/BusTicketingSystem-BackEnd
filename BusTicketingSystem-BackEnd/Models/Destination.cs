using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
    public class Destination
    {
        public int DestinationId { get; set; }

        [Required]
        [MaxLength(150)]
        public string DestinationName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
