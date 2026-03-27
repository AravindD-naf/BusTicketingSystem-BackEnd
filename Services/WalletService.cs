using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public WalletService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>Gets the wallet for a user, creating one if it doesn't exist yet.</summary>
        public async Task<WalletWithTransactionsDto> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await GetOrCreateAsync(userId);
            return MapToFullDto(wallet);
        }

        public async Task<WalletResponseDto> TopUpAsync(
            int userId, decimal amount, string paymentMethod, string ipAddress)
        {
            if (amount <= 0)
                throw new ValidationException("Top-up amount must be greater than zero.");
            if (amount > 50000)
                throw new ValidationException("Maximum top-up amount is ₹50,000 per transaction.");

            var wallet = await GetOrCreateAsync(userId);
            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                Type        = WalletTransactionType.Credit,
                Amount      = amount,
                BalanceAfter = wallet.Balance,
                Description = $"Wallet top-up via {paymentMethod}",
                ReferenceId = paymentMethod,
                CreatedAt   = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userId, "WALLET_TOPUP", "Wallet",
                wallet.WalletId.ToString(), null,
                new { amount, paymentMethod, balanceAfter = wallet.Balance }, ipAddress);

            return MapToDto(wallet);
        }

        public async Task<WalletResponseDto> DebitAsync(
            int userId, decimal amount, string description, string? referenceId, string ipAddress)
        {
            if (amount <= 0)
                throw new ValidationException("Debit amount must be greater than zero.");

            var wallet = await GetOrCreateAsync(userId);

            if (wallet.Balance < amount)
                throw new ValidationException(
                    $"Insufficient wallet balance. Available: ₹{wallet.Balance:F2}, Required: ₹{amount:F2}");

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new WalletTransaction
            {
                WalletId     = wallet.WalletId,
                Type         = WalletTransactionType.Debit,
                Amount       = amount,
                BalanceAfter = wallet.Balance,
                Description  = description,
                ReferenceId  = referenceId,
                CreatedAt    = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userId, "WALLET_DEBIT", "Wallet",
                wallet.WalletId.ToString(), null,
                new { amount, description, referenceId, balanceAfter = wallet.Balance }, ipAddress);

            return MapToDto(wallet);
        }

        public async Task<WalletResponseDto> CreditAsync(
            int userId, decimal amount, string description, string? referenceId, string ipAddress)
        {
            if (amount <= 0)
                throw new ValidationException("Credit amount must be greater than zero.");

            var wallet = await GetOrCreateAsync(userId);
            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new WalletTransaction
            {
                WalletId     = wallet.WalletId,
                Type         = WalletTransactionType.Credit,
                Amount       = amount,
                BalanceAfter = wallet.Balance,
                Description  = description,
                ReferenceId  = referenceId,
                CreatedAt    = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userId, "WALLET_CREDIT", "Wallet",
                wallet.WalletId.ToString(), null,
                new { amount, description, referenceId, balanceAfter = wallet.Balance }, ipAddress);

            return MapToDto(wallet);
        }

        public async Task<bool> HasSufficientBalanceAsync(int userId, decimal amount)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            return wallet != null && wallet.Balance >= amount;
        }

        // ── Helpers ──

        private async Task<Wallet> GetOrCreateAsync(int userId)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet != null) return wallet;

            // Auto-create wallet on first access
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

        private static WalletResponseDto MapToDto(Wallet w) => new()
        {
            WalletId  = w.WalletId,
            UserId    = w.UserId,
            Balance   = w.Balance,
            UpdatedAt = w.UpdatedAt
        };

        private static WalletWithTransactionsDto MapToFullDto(Wallet w) => new()
        {
            WalletId  = w.WalletId,
            Balance   = w.Balance,
            UpdatedAt = w.UpdatedAt,
            Transactions = w.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionResponseDto
                {
                    TransactionId = t.TransactionId,
                    Type          = t.Type.ToString(),
                    Amount        = t.Amount,
                    BalanceAfter  = t.BalanceAfter,
                    Description   = t.Description,
                    ReferenceId   = t.ReferenceId,
                    CreatedAt     = t.CreatedAt
                }).ToList()
        };
    }
}
