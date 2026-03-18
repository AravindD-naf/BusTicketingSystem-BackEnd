namespace BusTicketingSystem.Exceptions
{

    public class BookingException : Exception
    {
        public BookingException(string message) : base(message) { }
        public BookingException(string message, Exception innerException) : base(message, innerException) { }
    }


    public class InvalidScheduleException : BookingException
    {
        public InvalidScheduleException(string message) : base(message) { }
    }

    public class InsufficientSeatsException : BookingException
    {
        public InsufficientSeatsException(string message) : base(message) { }
    }

    public class SeatNotFoundException : BookingException
    {
        public SeatNotFoundException(string message) : base(message) { }
    }


    public class InvalidSeatStatusException : BookingException
    {
        public InvalidSeatStatusException(string message) : base(message) { }
    }


    public class UnauthorizedAccessException : BookingException
    {
        public UnauthorizedAccessException(string message) : base(message) { }
    }

    public class BookingNotFoundException : BookingException
    {
        public BookingNotFoundException(string message) : base(message) { }
    }

    public class BookingCancellationException : BookingException
    {
        public BookingCancellationException(string message) : base(message) { }
    }


    public class PaymentException : BookingException
    {
        public PaymentException(string message) : base(message) { }
    }

    public class PaymentTimeoutException : PaymentException
    {
        public PaymentTimeoutException(string message) : base(message) { }
    }


    public class RefundException : BookingException
    {
        public RefundException(string message) : base(message) { }
    }


    public class InvalidPassengerException : BookingException
    {
        public InvalidPassengerException(string message) : base(message) { }
    }
}
