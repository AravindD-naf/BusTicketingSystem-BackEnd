namespace BusTicketingSystem.DTOs
{
    public class ScheduleRequestDto
    {
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public DateTime TravelDate { get; set; }

        // Stored as strings to support overnight journeys (e.g. "36:15:00")
        // TimeSpan binding fails for hours > 23 at the model binding layer
        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public decimal Fare { get; set; } = 0;

        // Parsed TimeSpans — used internally by the service
        public TimeSpan DepartureTimeSpan =>
            TimeSpan.TryParse(DepartureTime, out var d) ? d : TimeSpan.Zero;

        public TimeSpan ArrivalTimeSpan =>
            TimeSpan.TryParse(ArrivalTime, out var a) ? a : TimeSpan.Zero;
    }
}