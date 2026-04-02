namespace BusTicketingSystem.DTOs.Responses
{
    public class SourceResponseDto
    {
        public int SourceId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
