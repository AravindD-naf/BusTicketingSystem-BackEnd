namespace BusTicketingSystem.DTOs.Requests
{
    public class ValidatePromoCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal BookingAmount { get; set; }
    }

    public class CreatePromoCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" | "Flat"
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; } = 0;
        public decimal MinBookingAmount { get; set; } = 0;
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntil { get; set; }
        public int MaxUsageCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePromoCodeRequest : CreatePromoCodeRequest { }
}
