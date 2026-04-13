using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly ApplicationDbContext _context;

        public WalletRepository(ApplicationDbContext context) => _context = context;

        public async Task<Wallet?> GetByUserIdAsync(int userId) =>
            await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

        public async Task<Wallet> GetOrCreateAsync(int userId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet != null) return wallet;

            wallet = new Wallet
            {
                UserId    = userId,
                Balance   = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        public async Task AddAsync(Wallet wallet) =>
            await _context.Wallets.AddAsync(wallet);

        public async Task AddTransactionAsync(WalletTransaction transaction) =>
            await _context.WalletTransactions.AddAsync(transaction);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
