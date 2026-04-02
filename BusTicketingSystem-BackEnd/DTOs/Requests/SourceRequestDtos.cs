namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateSourceRequest
    {
        public string SourceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateSourceRequest
    {
        public string SourceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
