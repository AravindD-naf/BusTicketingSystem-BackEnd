using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(int userId);
        Task<Wallet> GetOrCreateAsync(int userId);
        Task AddAsync(Wallet wallet);
        Task AddTransactionAsync(WalletTransaction transaction);
        Task SaveChangesAsync();
    }
}
