using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.DTOs.Requests
{
    public class UpdateBusRequest
    {
        [Required]
        public string BusType { get; set; } = string.Empty;

        [Required]
        [Range(10, 100)]
        public int TotalSeats { get; set; }

        [Required]
        public string OperatorName { get; set; } = string.Empty;

        public bool? IsActive { get; set; }
    }
}