namespace BusTicketingSystem.DTOs.Requests
{
    public class LockSeatsRequestDto
    {
        public int ScheduleId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();
    }

    public class ReleaseSeatsRequestDto
    {
        public int ScheduleId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();
    }
}
