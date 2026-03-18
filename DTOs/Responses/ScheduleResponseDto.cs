namespace BusTicketingSystem.DTOs
{
    //public class ScheduleResponseDto
    //{
    //    public int ScheduleId { get; set; }
    //    public int RouteId { get; set; }
    //    public int BusId { get; set; }
    //    public DateTime TravelDate { get; set; }
    //    public TimeSpan DepartureTime { get; set; }
    //    public TimeSpan ArrivalTime { get; set; }
    //    public int TotalSeats { get; set; }
    //    public int AvailableSeats { get; set; }
    //    public bool IsActive { get; set; }
    //}
    public class ScheduleResponseDto
    {
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public string? Source { get; set; }
        public string? Destination { get; set; }
        public int BusId { get; set; }
        public string? BusNumber { get; set; }
        public string? OperatorName { get; set; }
        public DateTime TravelDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public bool IsActive { get; set; }
    }
}