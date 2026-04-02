namespace BusTicketingSystem.DTOs.Responses
{
    public class WalletResponseDto
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionResponseDto
    {
        public int TransactionId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WalletWithTransactionsDto
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<WalletTransactionResponseDto> Transactions { get; set; } = new();
    }
}
