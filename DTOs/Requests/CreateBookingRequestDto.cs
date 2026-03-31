namespace BusTicketingSystem.DTOs.Requests
{
    public class CreateBookingRequestDto
    {
        public int ScheduleId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();

        // Boarding & drop point names (display strings from frontend)
        public string? BoardingPointName { get; set; }
        public string? DropPointName { get; set; }

        // Contact details
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        // Passenger details — one per seat
        public List<PassengerDetailDto> Passengers { get; set; } = new();

        public bool IsValid(out string error)
        {
            if (SeatNumbers == null || SeatNumbers.Count == 0) { error = "At least one seat must be selected."; return false; }
            if (SeatNumbers.Count > 6) { error = "Maximum 6 seats allowed per booking."; return false; }
            error = string.Empty;
            return true;
        }
    }
}
