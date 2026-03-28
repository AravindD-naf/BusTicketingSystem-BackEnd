using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly ApplicationDbContext _context;
        public PromoCodeService(ApplicationDbContext context) => _context = context;

        public async Task<PromoCodeValidationResponseDto> ValidateAsync(string code, decimal bookingAmount)
        {
            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper().Trim() && p.IsActive);
            if (promo == null) return Fail("Invalid promo code.");
            var now = DateTime.UtcNow;
            if (now < promo.ValidFrom || now > promo.ValidUntil) return Fail("This promo code has expired.");
            if (promo.MaxUsageCount > 0 && promo.UsedCount >= promo.MaxUsageCount) return Fail("This promo code has reached its usage limit.");
            if (bookingAmount < promo.MinBookingAmount) return Fail($"Minimum booking amount of ₹{promo.MinBookingAmount} required.");
            decimal discount = promo.DiscountType == DiscountType.Percentage
                ? Math.Round(bookingAmount * promo.DiscountValue / 100, 2) : promo.DiscountValue;
            if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount) discount = promo.MaxDiscountAmount;
            if (discount > bookingAmount) discount = bookingAmount;
            return new PromoCodeValidationResponseDto
            {
                IsValid = true, Code = promo.Code,
                DiscountType = promo.DiscountType.ToString(), DiscountValue = promo.DiscountValue,
                DiscountAmount = discount, FinalAmount = bookingAmount - discount,
                Message = promo.DiscountType == DiscountType.Percentage
                    ? $"{promo.DiscountValue}% discount applied — you save ₹{discount}!"
                    : $"₹{discount} flat discount applied!"
            };
        }

        public async Task IncrementUsageAsync(string code)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper().Trim());
            if (promo != null) { promo.UsedCount++; await _context.SaveChangesAsync(); }
        }

        public async Task<List<PromoCodeResponseDto>> GetAllAsync() =>
            await _context.PromoCodes.OrderByDescending(p => p.CreatedAt).Select(p => MapToDto(p)).ToListAsync();

        public async Task<List<PromoCodeResponseDto>> GetActiveAsync() =>
            await _context.PromoCodes
                .Where(p => p.IsActive && p.ValidFrom <= DateTime.UtcNow && p.ValidUntil >= DateTime.UtcNow)
                .OrderBy(p => p.ValidUntil)
                .Select(p => MapToDto(p))
                .ToListAsync();

        public async Task<PromoCodeResponseDto> CreateAsync(CreatePromoCodeRequest req)
        {
            var code = req.Code.Trim().ToUpper();
            if (await _context.PromoCodes.AnyAsync(p => p.Code == code))
                throw new ValidationException($"Promo code '{code}' already exists.");
            var promo = new PromoCode
            {
                Code = code,
                DiscountType = req.DiscountType == "Flat" ? DiscountType.Flat : DiscountType.Percentage,
                DiscountValue = req.DiscountValue,
                MaxDiscountAmount = req.MaxDiscountAmount,
                MinBookingAmount = req.MinBookingAmount,
                ValidFrom = req.ValidFrom,
                ValidUntil = req.ValidUntil,
                MaxUsageCount = req.MaxUsageCount,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();
            return MapToDto(promo);
        }

        public async Task<PromoCodeResponseDto> UpdateAsync(int id, UpdatePromoCodeRequest req)
        {
            var promo = await _context.PromoCodes.FindAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());
            var code = req.Code.Trim().ToUpper();
            if (await _context.PromoCodes.AnyAsync(p => p.Code == code && p.PromoCodeId != id))
                throw new ValidationException($"Promo code '{code}' already exists.");
            promo.Code = code;
            promo.DiscountType = req.DiscountType == "Flat" ? DiscountType.Flat : DiscountType.Percentage;
            promo.DiscountValue = req.DiscountValue;
            promo.MaxDiscountAmount = req.MaxDiscountAmount;
            promo.MinBookingAmount = req.MinBookingAmount;
            promo.ValidFrom = req.ValidFrom;
            promo.ValidUntil = req.ValidUntil;
            promo.MaxUsageCount = req.MaxUsageCount;
            promo.IsActive = req.IsActive;
            await _context.SaveChangesAsync();
            return MapToDto(promo);
        }

        public async Task DeleteAsync(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());
            _context.PromoCodes.Remove(promo);
            await _context.SaveChangesAsync();
        }

        public async Task<PromoCodeResponseDto?> ToggleActiveAsync(int id)
        {
            var promo = await _context.PromoCodes.FindAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());
            promo.IsActive = !promo.IsActive;
            await _context.SaveChangesAsync();
            return MapToDto(promo);
        }

        private static PromoCodeResponseDto MapToDto(PromoCode p) => new()
        {
            PromoCodeId = p.PromoCodeId, Code = p.Code,
            DiscountType = p.DiscountType.ToString(), DiscountValue = p.DiscountValue,
            MaxDiscountAmount = p.MaxDiscountAmount, MinBookingAmount = p.MinBookingAmount,
            ValidFrom = p.ValidFrom, ValidUntil = p.ValidUntil,
            MaxUsageCount = p.MaxUsageCount, UsedCount = p.UsedCount,
            IsActive = p.IsActive, CreatedAt = p.CreatedAt
        };

        private static PromoCodeValidationResponseDto Fail(string message) => new() { IsValid = false, Message = message };
    }
}
