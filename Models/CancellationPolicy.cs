using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
   
    public class CancellationPolicy
    {
        [Key]
        public int PolicyId { get; set; }

   
        [Required]
        [MaxLength(100)]
        public string PolicyName { get; set; } = "Standard";

        [Required]
        public int HoursBeforeDeparture { get; set; }


        [Required]
        public int RefundPercentage { get; set; }

        [Required]
        public int CancellationFeePercentage { get; set; }


        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;


        public bool IsActive { get; set; } = true;


        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
