using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IWalletService
    {
        Task<WalletWithTransactionsDto> GetOrCreateWalletAsync(int userId);
        Task<WalletResponseDto> TopUpAsync(int userId, decimal amount, string paymentMethod, string ipAddress);
        Task<WalletResponseDto> DebitAsync(int userId, decimal amount, string description, string? referenceId, string ipAddress);
        Task<WalletResponseDto> CreditAsync(int userId, decimal amount, string description, string? referenceId, string ipAddress);
        Task<bool> HasSufficientBalanceAsync(int userId, decimal amount);
    }
}
