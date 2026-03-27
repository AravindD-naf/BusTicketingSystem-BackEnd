using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly ApplicationDbContext _context;

        public PromoCodeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PromoCodeValidationResponseDto> ValidateAsync(string code, decimal bookingAmount)
        {
            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper().Trim() && p.IsActive);

            if (promo == null)
                return Fail("Invalid promo code.");

            var now = DateTime.UtcNow;
            if (now < promo.ValidFrom || now > promo.ValidUntil)
                return Fail("This promo code has expired.");

            if (promo.MaxUsageCount > 0 && promo.UsedCount >= promo.MaxUsageCount)
                return Fail("This promo code has reached its usage limit.");

            if (bookingAmount < promo.MinBookingAmount)
                return Fail($"Minimum booking amount of ₹{promo.MinBookingAmount} required for this code.");

            decimal discount = promo.DiscountType == DiscountType.Percentage
                ? Math.Round(bookingAmount * promo.DiscountValue / 100, 2)
                : promo.DiscountValue;

            // Apply cap
            if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount)
                discount = promo.MaxDiscountAmount;

            // Discount cannot exceed booking amount
            if (discount > bookingAmount)
                discount = bookingAmount;

            return new PromoCodeValidationResponseDto
            {
                IsValid = true,
                Code = promo.Code,
                DiscountType = promo.DiscountType.ToString(),
                DiscountValue = promo.DiscountValue,
                DiscountAmount = discount,
                FinalAmount = bookingAmount - discount,
                Message = promo.DiscountType == DiscountType.Percentage
                    ? $"{promo.DiscountValue}% discount applied — you save ₹{discount}!"
                    : $"₹{discount} flat discount applied!"
            };
        }

        public async Task IncrementUsageAsync(string code)
        {
            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper().Trim());
            if (promo != null)
            {
                promo.UsedCount++;
                await _context.SaveChangesAsync();
            }
        }

        private static PromoCodeValidationResponseDto Fail(string message) =>
            new() { IsValid = false, Message = message };
    }
}
