namespace BusTicketingSystem.Models.Enums
{

    public enum BookingStatus
    {
  
        Pending = 0,

        PaymentProcessing = 1,

        Confirmed = 2,

        PaymentFailed = 3,

        Cancelled = 4,

        Expired = 5,

        Refunded = 6
    }
}
