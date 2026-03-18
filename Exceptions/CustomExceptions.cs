namespace BusTicketingSystem.Exceptions
{
   
    public class ResourceNotFoundException : ApplicationException
    {
        public ResourceNotFoundException(
            string resourceName,
            string resourceId = null,
            Exception innerException = null)
            : base(
                userMessage: $"{resourceName} not found",
                errorCode: $"{resourceName.ToUpper()}_NOT_FOUND",
                statusCode: 404,
                internalMessage: $"{resourceName} with ID '{resourceId}' not found",
                innerException: innerException)
        {
            if (!string.IsNullOrEmpty(resourceId))
            {
                AddContextData("resourceId", resourceId);
            }
            AddContextData("resourceName", resourceName);
        }
    }


    public class ConflictException : ApplicationException
    {
        public ConflictException(
            string message,
            string errorCode = "CONFLICT_001",
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: errorCode,
                statusCode: 409,
                internalMessage: message,
                innerException: innerException)
        {
        }
    }


    public class ValidationException : ApplicationException
    {
        public ValidationException(
            string message,
            string errorCode = "VAL_001",
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: errorCode,
                statusCode: 400,
                internalMessage: message,
                innerException: innerException)
        {
        }


        public static ValidationException ForField(string fieldName, string message)
        {
            var ex = new ValidationException($"Validation failed for field: {fieldName}");
            ex.AddError(fieldName, message);
            return ex;
        }

        public static ValidationException ForFields(Dictionary<string, string> errors)
        {
            var ex = new ValidationException("Validation failed");
            foreach (var error in errors)
            {
                ex.AddError(error.Key, error.Value);
            }
            return ex;
        }
    }


    public class PaymentOperationException : ApplicationException
    {
        public enum PaymentErrorType
        {
            InvalidCard,
            InsufficientFunds,
            CardExpired,
            InvalidAmount,
            ProcessingError,
            TimeoutError
        }

        public PaymentErrorType ErrorType { get; set; }

        public PaymentOperationException(
            string message,
            PaymentErrorType errorType = PaymentErrorType.ProcessingError,
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: $"PAYMENT_{errorType}",
                statusCode: GetStatusCode(errorType),
                internalMessage: message,
                innerException: innerException)
        {
            ErrorType = errorType;
            AddContextData("paymentErrorType", errorType.ToString());
        }

        private static int GetStatusCode(PaymentErrorType type)
        {
            return type switch
            {
                PaymentErrorType.InvalidAmount => 400,
                PaymentErrorType.TimeoutError => 408,
                PaymentErrorType.ProcessingError => 500,
                _ => 402
            };
        }
    }


    public class RefundOperationException : ApplicationException
    {
        public enum RefundErrorType
        {
            InvalidRefund,
            AlreadyRefunded,
            RefundExpired,
            ProcessingError,
            InvalidAmount
        }

        public RefundErrorType ErrorType { get; set; }

        public RefundOperationException(
            string message,
            RefundErrorType errorType = RefundErrorType.ProcessingError,
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: $"REFUND_{errorType}",
                statusCode: GetStatusCode(errorType),
                internalMessage: message,
                innerException: innerException)
        {
            ErrorType = errorType;
            AddContextData("refundErrorType", errorType.ToString());
        }

        private static int GetStatusCode(RefundErrorType type)
        {
            return type switch
            {
                RefundErrorType.InvalidRefund => 400,
                RefundErrorType.InvalidAmount => 400,
                RefundErrorType.AlreadyRefunded => 409,
                RefundErrorType.RefundExpired => 410,
                RefundErrorType.ProcessingError => 500,
                _ => 500
            };
        }
    }


    public class SeatOperationException : ApplicationException
    {
        public enum SeatErrorType
        {
            SeatNotAvailable,
            SeatNotLocked,
            SeatLockExpired,
            InvalidSeatNumber,
            InvalidLockOperation,
            SeatAlreadyBooked
        }

        public SeatErrorType ErrorType { get; set; }

        public SeatOperationException(
            string message,
            SeatErrorType errorType = SeatErrorType.InvalidLockOperation,
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: $"SEAT_{errorType}",
                statusCode: GetStatusCode(errorType),
                internalMessage: message,
                innerException: innerException)
        {
            ErrorType = errorType;
            AddContextData("seatErrorType", errorType.ToString());
        }

        private static int GetStatusCode(SeatErrorType type)
        {
            return type switch
            {
                SeatErrorType.SeatNotAvailable => 409,
                SeatErrorType.SeatNotLocked => 400,
                SeatErrorType.SeatLockExpired => 410,
                SeatErrorType.InvalidSeatNumber => 400,
                SeatErrorType.SeatAlreadyBooked => 409,
                _ => 400
            };
        }
    }

 
    public class BookingOperationException : ApplicationException
    {
        public enum BookingErrorType
        {
            InvalidBooking,
            BookingExpired,
            InsufficientSeats,
            InvalidSchedule,
            InvalidPassengerCount,
            BookingNotFound,
            InvalidBookingStatus
        }

        public BookingErrorType ErrorType { get; set; }

        public BookingOperationException(
            string message,
            BookingErrorType errorType = BookingErrorType.InvalidBooking,
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: $"BOOKING_{errorType}",
                statusCode: GetStatusCode(errorType),
                internalMessage: message,
                innerException: innerException)
        {
            ErrorType = errorType;
            AddContextData("bookingErrorType", errorType.ToString());
        }

        private static int GetStatusCode(BookingErrorType type)
        {
            return type switch
            {
                BookingErrorType.InvalidBooking => 400,
                BookingErrorType.BookingExpired => 410,
                BookingErrorType.InsufficientSeats => 409,
                BookingErrorType.InvalidSchedule => 400,
                BookingErrorType.InvalidPassengerCount => 400,
                BookingErrorType.BookingNotFound => 404,
                BookingErrorType.InvalidBookingStatus => 409,
                _ => 400
            };
        }
    }


    public class ConcurrencyException : ApplicationException
    {
        public ConcurrencyException(
            string message = "Resource has been modified by another user",
            Exception innerException = null)
            : base(
                userMessage: message,
                errorCode: "CONCURRENCY_001",
                statusCode: 409,
                internalMessage: "Optimistic concurrency violation occurred",
                innerException: innerException)
        {
        }
    }


    public class DatabaseException : ApplicationException
    {
        public DatabaseException(
            string message = "A database error occurred",
            Exception innerException = null)
            : base(
                userMessage: "An error occurred while processing your request",
                errorCode: "DB_ERROR_001",
                statusCode: 500,
                internalMessage: message,
                innerException: innerException)
        {
        }
    }


    public class ExternalServiceException : ApplicationException
    {
        public string ServiceName { get; set; }

        public ExternalServiceException(
            string serviceName,
            string message = "External service unavailable",
            Exception innerException = null)
            : base(
                userMessage: "Service temporarily unavailable",
                errorCode: "SERVICE_UNAVAILABLE",
                statusCode: 503,
                internalMessage: message,
                innerException: innerException)
        {
            ServiceName = serviceName;
            AddContextData("serviceName", serviceName);
        }
    }


    public class OperationTimeoutException : ApplicationException
    {
        public OperationTimeoutException(
            string message = "Operation timed out",
            Exception innerException = null)
            : base(
                userMessage: "The operation took too long to complete. Please try again.",
                errorCode: "TIMEOUT_001",
                statusCode: 408,
                internalMessage: message,
                innerException: innerException)
        {
        }
    }
}
