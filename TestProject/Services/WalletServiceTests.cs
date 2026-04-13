using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Repositories;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<IAuditService> _auditMock = new();

    private WalletService CreateSut(BusTicketingSystem.Data.ApplicationDbContext ctx)
    {
        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        return new WalletService(new WalletRepository(ctx), _auditMock.Object);
    }

    // ── GetOrCreateWalletAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateWalletAsync_NoExistingWallet_CreatesAndReturns()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetOrCreateWalletAsync(userId: 99);

        // Assert
        result.Balance.Should().Be(0);
        ctx.Wallets.Should().ContainSingle(w => w.UserId == 99);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_ExistingWallet_ReturnsSameWallet()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 5, balance: 2500m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();

        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetOrCreateWalletAsync(userId: 5);

        // Assert
        result.Balance.Should().Be(2500m);
        ctx.Wallets.Count(w => w.UserId == 5).Should().Be(1, "no duplicate wallets");
    }

    // ── TopUpAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopUpAsync_ValidAmount_IncreasesBalance()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 10, balance: 1000m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.TopUpAsync(10, amount: 500m, paymentMethod: "UPI", ipAddress: "127.0.0.1");

        // Assert
        result.Balance.Should().Be(1500m);
        ctx.WalletTransactions.Should().ContainSingle(t =>
            t.WalletId == wallet.WalletId && t.Type == WalletTransactionType.Credit && t.Amount == 500m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task TopUpAsync_NonPositiveAmount_ThrowsValidationException(decimal amount)
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.TopUpAsync(1, amount, "UPI", "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task TopUpAsync_ExceedsMaximum_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.TopUpAsync(1, amount: 60_000m, paymentMethod: "Card", ipAddress: "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*50,000*");
    }

    // ── DebitAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DebitAsync_SufficientBalance_DecreasesBalance()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 20, balance: 3000m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.DebitAsync(20, 1000m, "Bus booking #5", "5", "127.0.0.1");

        // Assert
        result.Balance.Should().Be(2000m);
        ctx.WalletTransactions.Should().ContainSingle(t =>
            t.Type == WalletTransactionType.Debit && t.Amount == 1000m);
    }

    [Fact]
    public async Task DebitAsync_InsufficientBalance_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 21, balance: 200m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.DebitAsync(21, 500m, "desc", null, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Insufficient wallet balance*");
    }

    [Fact]
    public async Task DebitAsync_ZeroAmount_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.DebitAsync(1, 0m, "desc", null, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── CreditAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_ValidAmount_IncreasesBalance()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 30, balance: 0m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.CreditAsync(30, 250m, "Refund for booking #1", "1", "127.0.0.1");

        // Assert
        result.Balance.Should().Be(250m);
        ctx.WalletTransactions.Should().ContainSingle(t =>
            t.Type == WalletTransactionType.Credit && t.Amount == 250m);
    }

    [Fact]
    public async Task CreditAsync_NegativeAmount_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.CreditAsync(1, -50m, "desc", null, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── HasSufficientBalanceAsync ─────────────────────────────────────────────

    [Fact]
    public async Task HasSufficientBalanceAsync_EnoughBalance_ReturnsTrue()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 40, balance: 1000m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.HasSufficientBalanceAsync(40, 500m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_NoWallet_ReturnsFalse()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.HasSufficientBalanceAsync(userId: 999, amount: 100m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_ExactBalance_ReturnsTrue()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var wallet = TestDataBuilder.WalletWithBalance(userId: 50, balance: 500m);
        ctx.Wallets.Add(wallet);
        ctx.SaveChanges();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.HasSufficientBalanceAsync(50, 500m);

        // Assert
        result.Should().BeTrue("exact balance should be sufficient");
    }

    // ── Balance audit trail ───────────────────────────────────────────────────

    [Fact]
    public async Task TopUp_ThenDebit_BalanceIsAccurate()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act
        await sut.GetOrCreateWalletAsync(userId: 60);
        await sut.TopUpAsync(60, 2000m, "Card", "127.0.0.1");
        await sut.DebitAsync(60, 750m, "booking", null, "127.0.0.1");
        var walletState = await sut.GetOrCreateWalletAsync(userId: 60);

        // Assert
        walletState.Balance.Should().Be(1250m);
        walletState.Transactions.Should().HaveCount(2);
    }
}
