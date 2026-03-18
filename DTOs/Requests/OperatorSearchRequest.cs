namespace BusTicketingSystem.DTOs.Requests
{
    public class OperatorSearchRequest : PaginationRequest
    {
        public string OperatorName { get; set; } = string.Empty;
    }
}
