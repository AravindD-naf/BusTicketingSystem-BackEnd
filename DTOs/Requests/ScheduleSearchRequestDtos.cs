namespace BusTicketingSystem.DTOs.Requests
{
    public class CitySearchRequest : PaginationRequest
    {
        public string City { get; set; } = string.Empty;
    }

    public class ScheduleSearchRequest : PaginationRequest
    {
        public string FromCity { get; set; } = string.Empty;
        public string ToCity { get; set; } = string.Empty;
        public DateTime TravelDate { get; set; }
    }
}
