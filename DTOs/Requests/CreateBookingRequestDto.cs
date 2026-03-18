namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateBookingRequestDto
    {
        public int ScheduleId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();
    }
}
