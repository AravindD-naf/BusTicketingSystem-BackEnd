using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;

namespace BusTicketingSystem.Services
{
    public class PromoCodeService : IPromoCodeService
    {
        private readonly IPromoCodeRepository _promoCodeRepository;

        public PromoCodeService(IPromoCodeRepository promoCodeRepository) =>
            _promoCodeRepository = promoCodeRepository;

        public async Task<PromoCodeValidationResponseDto> ValidateAsync(string code, decimal bookingAmount)
        {
            var promo = await _promoCodeRepository.GetByCodeAsync(code);
            if (promo == null || !promo.IsActive) return Fail("Invalid promo code.");

            var now = DateTime.UtcNow;
            if (now < promo.ValidFrom || now > promo.ValidUntil) return Fail("This promo code has expired.");
            if (promo.MaxUsageCount > 0 && promo.UsedCount >= promo.MaxUsageCount) return Fail("This promo code has reached its usage limit.");
            if (bookingAmount < promo.MinBookingAmount) return Fail($"Minimum booking amount of ₹{promo.MinBookingAmount} required.");

            decimal discount = promo.DiscountType == DiscountType.Percentage
                ? Math.Round(bookingAmount * promo.DiscountValue / 100, 2)
                : promo.DiscountValue;

            if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount) discount = promo.MaxDiscountAmount;
            if (discount > bookingAmount) discount = bookingAmount;

            return new PromoCodeValidationResponseDto
            {
                IsValid       = true,
                Code          = promo.Code,
                DiscountType  = promo.DiscountType.ToString(),
                DiscountValue = promo.DiscountValue,
                DiscountAmount = discount,
                FinalAmount   = bookingAmount - discount,
                Message       = promo.DiscountType == DiscountType.Percentage
                    ? $"{promo.DiscountValue}% discount applied — you save ₹{discount}!"
                    : $"₹{discount} flat discount applied!"
            };
        }

        public async Task IncrementUsageAsync(string code)
        {
            var promo = await _promoCodeRepository.GetByCodeAsync(code);
            if (promo == null) return;
            promo.UsedCount++;
            await _promoCodeRepository.UpdateAsync(promo);
            await _promoCodeRepository.SaveChangesAsync();
        }

        public async Task<List<PromoCodeResponseDto>> GetAllAsync()
        {
            var promos = await _promoCodeRepository.GetAllAsync();
            return promos.Select(MapToDto).ToList();
        }

        public async Task<List<PromoCodeResponseDto>> GetActiveAsync()
        {
            var promos = await _promoCodeRepository.GetActiveAsync();
            return promos.Select(MapToDto).ToList();
        }

        public async Task<PromoCodeResponseDto> CreateAsync(CreatePromoCodeRequest req)
        {
            var code = req.Code.Trim().ToUpper();
            if (await _promoCodeRepository.CodeExistsAsync(code))
                throw new ValidationException($"Promo code '{code}' already exists.");

            var promo = new PromoCode
            {
                Code             = code,
                DiscountType     = req.DiscountType == "Flat" ? DiscountType.Flat : DiscountType.Percentage,
                DiscountValue    = req.DiscountValue,
                MaxDiscountAmount = req.MaxDiscountAmount,
                MinBookingAmount = req.MinBookingAmount,
                ValidFrom        = req.ValidFrom,
                ValidUntil       = req.ValidUntil,
                MaxUsageCount    = req.MaxUsageCount,
                IsActive         = req.IsActive,
                CreatedAt        = DateTime.UtcNow
            };

            await _promoCodeRepository.AddAsync(promo);
            await _promoCodeRepository.SaveChangesAsync();
            return MapToDto(promo);
        }

        public async Task<PromoCodeResponseDto> UpdateAsync(int id, UpdatePromoCodeRequest req)
        {
            var promo = await _promoCodeRepository.GetByIdAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());

            var code = req.Code.Trim().ToUpper();
            if (await _promoCodeRepository.CodeExistsAsync(code, excludeId: id))
                throw new ValidationException($"Promo code '{code}' already exists.");

            promo.Code             = code;
            promo.DiscountType     = req.DiscountType == "Flat" ? DiscountType.Flat : DiscountType.Percentage;
            promo.DiscountValue    = req.DiscountValue;
            promo.MaxDiscountAmount = req.MaxDiscountAmount;
            promo.MinBookingAmount = req.MinBookingAmount;
            promo.ValidFrom        = req.ValidFrom;
            promo.ValidUntil       = req.ValidUntil;
            promo.MaxUsageCount    = req.MaxUsageCount;
            promo.IsActive         = req.IsActive;

            await _promoCodeRepository.UpdateAsync(promo);
            await _promoCodeRepository.SaveChangesAsync();
            return MapToDto(promo);
        }

        public async Task DeleteAsync(int id)
        {
            var promo = await _promoCodeRepository.GetByIdAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());
            await _promoCodeRepository.DeleteAsync(promo);
            await _promoCodeRepository.SaveChangesAsync();
        }

        public async Task<PromoCodeResponseDto?> ToggleActiveAsync(int id)
        {
            var promo = await _promoCodeRepository.GetByIdAsync(id)
                ?? throw new ResourceNotFoundException("PromoCode", id.ToString());
            promo.IsActive = !promo.IsActive;
            await _promoCodeRepository.UpdateAsync(promo);
            await _promoCodeRepository.SaveChangesAsync();
            return MapToDto(promo);
        }

        private static PromoCodeResponseDto MapToDto(PromoCode p) => new()
        {
            PromoCodeId      = p.PromoCodeId,
            Code             = p.Code,
            DiscountType     = p.DiscountType.ToString(),
            DiscountValue    = p.DiscountValue,
            MaxDiscountAmount = p.MaxDiscountAmount,
            MinBookingAmount = p.MinBookingAmount,
            ValidFrom        = p.ValidFrom,
            ValidUntil       = p.ValidUntil,
            MaxUsageCount    = p.MaxUsageCount,
            UsedCount        = p.UsedCount,
            IsActive         = p.IsActive,
            CreatedAt        = p.CreatedAt
        };

        private static PromoCodeValidationResponseDto Fail(string message) =>
            new() { IsValid = false, Message = message };
    }
}
