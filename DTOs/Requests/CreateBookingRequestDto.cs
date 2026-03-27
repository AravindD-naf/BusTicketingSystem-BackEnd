namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateBookingRequestDto
    {
        public int ScheduleId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();

        public bool IsValid(out string error)
        {
            if (SeatNumbers == null || SeatNumbers.Count == 0) { error = "At least one seat must be selected."; return false; }
            if (SeatNumbers.Count > 6) { error = "Maximum 6 seats allowed per booking."; return false; }
            error = string.Empty;
            return true;
        }
    }
}
