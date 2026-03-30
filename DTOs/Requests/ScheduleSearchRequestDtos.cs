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

        // Parsed as UTC date � safe to compare against DB TravelDate
        public DateTime TravelDateUtc =>
            DateTime.TryParse(TravelDate, out var d)
                ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)
                : DateTime.UtcNow.Date;

        // ADD these properties to the existing ScheduleSearchRequest class:
        public List<string>? BusTypes { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<string>? DepartureTimes { get; set; }  // "morning","afternoon","evening","night"
        public List<string>? Operators { get; set; }
        public double? MinRating { get; set; }
        public string? SortBy { get; set; }  // "departure","arrival","price_asc","price_desc","duration","rating"
        public new int PageNumber { get; set; } = 1;
        public new int PageSize { get; set; } = 20;

    }
}