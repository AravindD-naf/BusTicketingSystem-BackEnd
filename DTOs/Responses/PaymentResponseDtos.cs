using BusTicketingSystem.Models.Enums;

namespace BusTicketingSystem.DTOs.Responses
{
  
    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }


    public class RefundResponseDto
    {
        public int RefundId { get; set; }
        public int BookingId { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CancellationFee { get; set; }
        public int RefundPercentage { get; set; }
        public RefundStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class BookingWithPaymentResponseDto
    {
        public int BookingId { get; set; }
        public int ScheduleId { get; set; }
        public int NumberOfSeats { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime? LastStatusChangeAt { get; set; }

        // Payment Info
        public PaymentResponseDto? Payment { get; set; }

        // Passenger Info
        public List<PassengerResponseDto> Passengers { get; set; } = new();
    }

    /// Passenger response DTO
    public class PassengerResponseDto
    {
        public int PassengerId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string IdType { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public int Age { get; set; }
        public string SpecialRequirements { get; set; } = string.Empty;
    }
}
