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

        // Stored as string to avoid timezone conversion issues.
        // The frontend sends "YYYY-MM-DD". We parse it as UTC date only.
        public string TravelDate { get; set; } = string.Empty;

        // Parsed as UTC date — safe to compare against DB TravelDate
        public DateTime TravelDateUtc =>
            DateTime.TryParse(TravelDate, out var d)
                ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)
                : DateTime.UtcNow.Date;
    }
}