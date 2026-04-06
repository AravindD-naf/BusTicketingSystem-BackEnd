namespace BusTicketingSystem.DTOs.Responses
{
    public class DestinationResponseDto
    {
        public int DestinationId { get; set; }
        public string DestinationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
