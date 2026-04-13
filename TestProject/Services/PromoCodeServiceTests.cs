using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Models;
using BusTicketingSystem.Repositories;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;

namespace BusTicketingSystem.Tests.Services;

/// <summary>
/// PromoCodeService now uses IPromoCodeRepository, exercised here
/// with the EF InMemory provider via PromoCodeRepository.
/// </summary>
public class PromoCodeServiceTests
{
    private ApplicationDbContext CreateContext() => DbContextFactory.CreateInMemory();

    private static PromoCodeService CreateSut(ApplicationDbContext ctx) =>
        new PromoCodeService(new PromoCodeRepository(ctx));

    private static void SeedPromos(ApplicationDbContext ctx)
    {
        ctx.PromoCodes.AddRange(
            TestDataBuilder.ActivePercentagePromo(id: 1),
            TestDataBuilder.ActiveFlatPromo(id: 2),
            TestDataBuilder.ExpiredPromo(id: 3));
        ctx.SaveChanges();
    }

    // ── ValidateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidPercentagePromo_ReturnsCorrectDiscount()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ValidateAsync("TEST20", bookingAmount: 1000m);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Code.Should().Be("TEST20");
        result.DiscountAmount.Should().Be(200m);     // 20% of 1000
        result.FinalAmount.Should().Be(800m);
        result.DiscountType.Should().Be("Percentage");
    }

    [Fact]
    public async Task ValidateAsync_ValidFlatPromo_ReturnsCorrectDiscount()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ValidateAsync("FLAT150", bookingAmount: 800m);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(150m);
        result.FinalAmount.Should().Be(650m);
    }

    [Fact]
    public async Task ValidateAsync_CaseInsensitiveCode_Validates()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act — use lowercase version of code
        var result = await sut.ValidateAsync("test20", bookingAmount: 1000m);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidCode_ReturnsFail()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ValidateAsync("GHOST", bookingAmount: 1000m);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Invalid promo code");
    }

    [Fact]
    public async Task ValidateAsync_ExpiredPromo_ReturnsFail()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ValidateAsync("EXPIRED", bookingAmount: 500m);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidateAsync_AmountBelowMinimum_ReturnsFail()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act — TEST20 requires min ₹300; we send ₹100
        var result = await sut.ValidateAsync("TEST20", bookingAmount: 100m);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Minimum booking amount");
    }

    [Fact]
    public async Task ValidateAsync_UsageLimitReached_ReturnsFail()
    {
        // Arrange
        await using var ctx = CreateContext();
        var promo = TestDataBuilder.ActivePercentagePromo(id: 10);
        promo.MaxUsageCount = 5;
        promo.UsedCount     = 5;   // limit reached
        ctx.PromoCodes.Add(promo);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ValidateAsync("TEST20", bookingAmount: 1000m);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("usage limit");
    }

    [Fact]
    public async Task ValidateAsync_DiscountExceedsBookingAmount_CappsAtBookingAmount()
    {
        // Arrange
        await using var ctx = CreateContext();
        var promo = new PromoCode
        {
            PromoCodeId = 20, Code = "BIG",
            DiscountType = DiscountType.Flat, DiscountValue = 1000m,
            MaxDiscountAmount = 0, MinBookingAmount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1), ValidUntil = DateTime.UtcNow.AddDays(10),
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        ctx.PromoCodes.Add(promo);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act — booking is only ₹300 but discount is ₹1000 flat
        var result = await sut.ValidateAsync("BIG", bookingAmount: 300m);

        // Assert — discount capped at booking amount
        result.IsValid.Should().BeTrue();
        result.DiscountAmount.Should().Be(300m);
        result.FinalAmount.Should().Be(0m);
    }

    // ── IncrementUsageAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task IncrementUsageAsync_ExistingCode_IncrementsCounter()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);
        var initialCount = ctx.PromoCodes.First(p => p.Code == "TEST20").UsedCount;

        // Act
        await sut.IncrementUsageAsync("TEST20");

        // Assert
        var updated = ctx.PromoCodes.First(p => p.Code == "TEST20");
        updated.UsedCount.Should().Be(initialCount + 1);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewCode_PersistsAndReturnsDto()
    {
        // Arrange
        await using var ctx = CreateContext();
        var sut = CreateSut(ctx);

        var request = new CreatePromoCodeRequest
        {
            Code             = "NEW50",
            DiscountType     = "Flat",
            DiscountValue    = 50,
            MaxDiscountAmount = 50,
            MinBookingAmount = 200,
            ValidFrom        = DateTime.UtcNow,
            ValidUntil       = DateTime.UtcNow.AddDays(30),
            IsActive         = true
        };

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.Code.Should().Be("NEW50");
        result.DiscountType.Should().Be("Flat");
        ctx.PromoCodes.Should().ContainSingle(p => p.Code == "NEW50");
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        var request = new CreatePromoCodeRequest
        {
            Code = "TEST20",   // duplicate
            DiscountType = "Percentage", DiscountValue = 10,
            ValidFrom = DateTime.UtcNow, ValidUntil = DateTime.UtcNow.AddDays(10)
        };

        // Act & Assert
        Func<Task> act = () => sut.CreateAsync(request);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*TEST20*already exists*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingPromo_UpdatesFields()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        var request = new UpdatePromoCodeRequest
        {
            Code          = "TEST20",
            DiscountType  = "Percentage",
            DiscountValue = 25,           // bumped from 20 → 25
            ValidFrom     = DateTime.UtcNow,
            ValidUntil    = DateTime.UtcNow.AddDays(60),
            IsActive      = true
        };

        // Act
        var result = await sut.UpdateAsync(1, request);

        // Assert
        result.DiscountValue.Should().Be(25);
        ctx.PromoCodes.First(p => p.PromoCodeId == 1).DiscountValue.Should().Be(25);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentPromo_ThrowsResourceNotFoundException()
    {
        // Arrange
        await using var ctx = CreateContext();
        var sut = CreateSut(ctx);

        var request = new UpdatePromoCodeRequest
        {
            Code = "X", DiscountType = "Flat", DiscountValue = 10,
            ValidFrom = DateTime.UtcNow, ValidUntil = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        Func<Task> act = () => sut.UpdateAsync(999, request);
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingPromo_RemovesFromDb()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        await sut.DeleteAsync(1);

        // Assert
        ctx.PromoCodes.Should().NotContain(p => p.PromoCodeId == 1);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentPromo_ThrowsResourceNotFoundException()
    {
        // Arrange
        await using var ctx = CreateContext();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.DeleteAsync(999);
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActiveAsync_ActivePromo_DeactivatesIt()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ToggleActiveAsync(1);   // TEST20 was active

        // Assert
        result!.IsActive.Should().BeFalse();
        ctx.PromoCodes.First(p => p.PromoCodeId == 1).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActiveAsync_InactivePromo_ActivatesIt()
    {
        // Arrange
        await using var ctx = CreateContext();
        var promo = TestDataBuilder.ActivePercentagePromo(id: 50);
        promo.IsActive = false;
        ctx.PromoCodes.Add(promo);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ToggleActiveAsync(50);

        // Assert
        result!.IsActive.Should().BeTrue();
    }

    // ── GetActiveAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyCurrentlyActivePromos()
    {
        // Arrange
        await using var ctx = CreateContext();
        SeedPromos(ctx);
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetActiveAsync();

        // Assert: only TEST20 and FLAT150 are active & within date range
        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.Code == "EXPIRED");
    }
}
