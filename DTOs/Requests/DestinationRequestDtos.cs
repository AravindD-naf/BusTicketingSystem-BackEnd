namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateDestinationRequest
    {
        public string DestinationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateDestinationRequest
    {
        public string DestinationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
