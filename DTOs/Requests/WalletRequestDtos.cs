namespace BusTicketingSystem.DTOs.Requests
{
    public class WalletTopUpRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class WalletDebitRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
    }

    public class WalletCreditRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
    }
}
