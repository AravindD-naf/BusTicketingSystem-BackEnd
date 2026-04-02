using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateBusRequest
    {
        [Required]
        public string BusNumber { get; set; } = string.Empty;

        [Required]
        public string BusType { get; set; } = string.Empty;

        [Required]
        [Range(1, 40, ErrorMessage = "Total seats must be between 1 and 40")]
        public int TotalSeats { get; set; }

        [Required]
        public string OperatorName { get; set; } = string.Empty;
    }
}