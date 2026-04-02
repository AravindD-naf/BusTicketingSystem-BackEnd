namespace BusTicketingSystem.DTOs.Responses
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public int ScheduleId { get; set; }
        public int NumberOfSeats { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;
        public BookingRefundDto? Refund { get; set; }

        // Route / Schedule details (for profile & my-bookings display)
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime? TravelDate { get; set; }
        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;

        // Bus details
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;

        // Seat numbers
        public List<string> SeatNumbers { get; set; } = new();

        // Promo code details
        public string? PromoCodeUsed { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PNR { get; set; } = string.Empty;

        // Whether the user has already rated this booking's bus
        public bool HasRated { get; set; } = false;

        // Boarding & drop points
        public string? BoardingPointName { get; set; }
        public string? DropPointName { get; set; }

        // Contact details
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        // Passenger details
        public List<PassengerSummaryDto> Passengers { get; set; } = new();
    }

    public class BookingDetailResponseDto
    {
        public int BookingId { get; set; }
        public int ScheduleId { get; set; }
        public int NumberOfSeats { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = string.Empty;

        // Route Details
        public int RouteId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        // Bus Details
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public double RatingAverage { get; set; }

        // Schedule Details
        public DateTime TravelDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }

        // Promo code details
        public string? PromoCodeUsed { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PNR { get; set; } = string.Empty;

        // Seat numbers
        public List<string> SeatNumbers { get; set; } = new();

        // Boarding & drop points
        public string? BoardingPointName { get; set; }
        public string? DropPointName { get; set; }

        // Contact details
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        // Passenger details
        public List<PassengerSummaryDto> Passengers { get; set; } = new();
    }

    public class PassengerSummaryDto
    {
        public string SeatNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Gender { get; set; }
    }

    public class BookingRefundDto
    {
        public int RefundId { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CancellationFee { get; set; }
        public double RefundPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
    }
}
