namespace BusTicketingSystem.DTOs
{
    public class ScheduleRequestDto
    {
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public DateTime TravelDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
    }
}