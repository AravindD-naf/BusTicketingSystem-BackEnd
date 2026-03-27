namespace BusTicketingSystem.DTOs.Requests
{
    public class ValidatePromoCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal BookingAmount { get; set; }
    }
}
