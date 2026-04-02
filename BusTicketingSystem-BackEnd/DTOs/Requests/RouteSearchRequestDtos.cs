namespace BusTicketingSystem.DTOs.Requests
{
    public class RouteSourceSearchRequest : PaginationRequest
    {
        public string Source { get; set; } = string.Empty;
    }

    public class RouteDestinationSearchRequest : PaginationRequest
    {
        public string Destination { get; set; } = string.Empty;
    }

    public class RouteAdvancedSearchRequest : PaginationRequest
    {
        public string? Source { get; set; }
        public string? Destination { get; set; }
    }
}
