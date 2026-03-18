namespace BusTicketingSystem.DTOs.Responses
{
    public class SeatResponseDto
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatStatus { get; set; } = string.Empty; // Available, Locked, Booked
        public int? LockedByUserId { get; set; }
        public DateTime? LockedAt { get; set; }
        public DateTime? LockedExpiresAt { get; set; }
    }

    public class SeatLayoutResponseDto
    {
        public int ScheduleId { get; set; }
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int LockedSeats { get; set; }
        public int BookedSeats { get; set; }
        public List<SeatResponseDto> Seats { get; set; } = new();
    }

    public class LockSeatsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> LockedSeatNumbers { get; set; } = new();
        public List<string> FailedSeatNumbers { get; set; } = new();
        public DateTime? LockExpiresAt { get; set; }
    }

    public class ReleaseSeatsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ReleasedSeatNumbers { get; set; } = new();
        public List<string> FailedSeatNumbers { get; set; } = new();
    }
}
