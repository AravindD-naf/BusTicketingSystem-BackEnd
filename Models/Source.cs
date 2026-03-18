using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
    public class Source
    {
        public int SourceId { get; set; }

        [Required]
        [MaxLength(150)]
        public string SourceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
