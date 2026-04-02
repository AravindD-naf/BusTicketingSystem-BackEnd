namespace BusTicketingSystem.DTOs.Responses
{
    public class AuditLogResponse
    {
        public int AuditId { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}