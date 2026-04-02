namespace BusTicketingSystem.DTOs.Requests
{
    public class AuditLogSearchRequest : PaginationRequest
    {
        public string? EntityName { get; set; }
        public int? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
