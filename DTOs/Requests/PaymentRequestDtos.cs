namespace BusTicketingSystem.DTOs.Requests
{

    public class InitiatePaymentRequestDto
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }


    //public class ProcessPaymentRequestDto
    //{
    //    public int BookingId { get; set; }
    //    public string PaymentMethod { get; set; } = string.Empty; 
    //    public decimal Amount { get; set; }
    //}


    public class ConfirmPaymentRequestDto
    {
        public int PaymentId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }


    public class CancelBookingWithRefundRequestDto
    {
        public int BookingId { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
    }


    public class ConfirmRefundRequestDto
    {
        public int RefundId { get; set; }
        public bool IsApproved { get; set; }
        public string Reason { get; set; } = string.Empty;
    }


    public class AddPassengerRequestDto
    {
        public int BookingId { get; set; }
        public List<PassengerDetailDto> Passengers { get; set; } = new();
    }

    public class PassengerDetailDto
    {
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
