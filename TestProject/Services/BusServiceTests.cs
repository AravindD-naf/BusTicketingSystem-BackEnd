using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class BusServiceTests
{
    private readonly Mock<IBusRepository>       _busRepoMock       = new();
    private readonly Mock<IAuditService>         _auditMock         = new();
    private readonly Mock<IHttpContextAccessor>  _httpAccessorMock  = new();
    private readonly Mock<IScheduleRepository>   _scheduleRepoMock  = new();
    private readonly BusService                  _sut;

    public BusServiceTests()
    {
        _sut = new BusService(
            _busRepoMock.Object,
            _auditMock.Object,
            _httpAccessorMock.Object,
            _scheduleRepoMock.Object);

        // Default: audit log is a no-op
        _auditMock
            .Setup(a => a.LogAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    // ── CreateBusAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBusAsync_UniqueBusNumber_ReturnsBusResponse()
    {
        // Arrange
        var request = new CreateBusRequest
        {
            BusNumber    = "tn01ab1234",  // lowercase — should be normalised
            BusType      = "AC Sleeper",
            TotalSeats   = 40,
            OperatorName = "Test Travels"
        };

        _busRepoMock
            .Setup(r => r.GetByBusNumberAsync("TN01AB1234"))
            .ReturnsAsync((Bus?)null);

        _busRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Bus>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateBusAsync(request, userId: 1, ipAddress: "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.BusNumber.Should().Be("TN01AB1234");
        result.BusType.Should().Be("AC Sleeper");
        result.TotalSeats.Should().Be(40);
        result.IsActive.Should().BeFalse("new buses are inactive until scheduled");
    }

    [Fact]
    public async Task CreateBusAsync_DuplicateBusNumber_ThrowsConflictException()
    {
        // Arrange
        var existing = TestDataBuilder.ActiveBus();
        _busRepoMock
            .Setup(r => r.GetByBusNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(existing);

        var request = new CreateBusRequest
        {
            BusNumber    = existing.BusNumber,
            BusType      = "AC",
            TotalSeats   = 20,
            OperatorName = "Dup Travels"
        };

        // Act
        Func<Task> act = () => _sut.CreateBusAsync(request, 1, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{existing.BusNumber}*already exists*");
    }

    [Fact]
    public async Task CreateBusAsync_DeletedBusWithSameNumber_IsAllowed()
    {
        // Arrange
        var deletedBus = TestDataBuilder.ActiveBus();
        deletedBus.IsDeleted = true;  // soft-deleted — number should be reusable

        _busRepoMock
            .Setup(r => r.GetByBusNumberAsync(It.IsAny<string>()))
            .ReturnsAsync(deletedBus);

        _busRepoMock.Setup(r => r.CreateAsync(It.IsAny<Bus>())).Returns(Task.CompletedTask);

        var request = new CreateBusRequest
        {
            BusNumber    = deletedBus.BusNumber,
            BusType      = "AC",
            TotalSeats   = 20,
            OperatorName = "New Travels"
        };

        // Act
        var result = await _sut.CreateBusAsync(request, 1, "127.0.0.1");

        // Assert
        result.Should().NotBeNull("re-using a soft-deleted bus number is allowed");
    }

    // ── GetBusByIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBusByIdAsync_ExistingId_ReturnsBusResponse()
    {
        // Arrange
        var bus = TestDataBuilder.ActiveBus(id: 5);
        _busRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(bus);

        // Act
        var result = await _sut.GetBusByIdAsync(5);

        // Assert
        result.BusId.Should().Be(5);
        result.OperatorName.Should().Be(bus.OperatorName);
    }

    [Fact]
    public async Task GetBusByIdAsync_NonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        _busRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bus?)null);

        // Act & Assert
        Func<Task> act = () => _sut.GetBusByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetAllBusesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllBusesAsync_ReturnsMappedPage()
    {
        // Arrange
        var buses = new List<Bus> { TestDataBuilder.ActiveBus(1), TestDataBuilder.ActiveBus(2) };
        _busRepoMock
            .Setup(r => r.GetPagedAsync(1, 10))
            .ReturnsAsync((buses, 2));

        // Act
        var (items, totalCount) = await _sut.GetAllBusesAsync(1, 10);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    // ── UpdateBusAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBusAsync_NoFutureSchedules_UpdatesBus()
    {
        // Arrange
        var bus = TestDataBuilder.ActiveBus(id: 3);
        _busRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(bus);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForBusAsync(3)).ReturnsAsync(false);
        _busRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Bus>())).Returns(Task.CompletedTask);

        var request = new UpdateBusRequest
        {
            BusType      = "Non-AC",
            TotalSeats   = 32,
            OperatorName = "Updated Travels",
            IsActive     = true
        };

        // Act
        Func<Task> act = () => _sut.UpdateBusAsync(3, request, userId: 1, ipAddress: "127.0.0.1");

        // Assert
        await act.Should().NotThrowAsync();
        bus.BusType.Should().Be("Non-AC");
        bus.TotalSeats.Should().Be(32);
        bus.OperatorName.Should().Be("Updated Travels");
    }

    [Fact]
    public async Task UpdateBusAsync_HasFutureSchedules_ThrowsConflictException()
    {
        // Arrange
        var bus = TestDataBuilder.ActiveBus(id: 4);
        _busRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(bus);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForBusAsync(4)).ReturnsAsync(true);

        var request = new UpdateBusRequest
        {
            BusType = "Non-AC", TotalSeats = 32, OperatorName = "Travels"
        };

        // Act & Assert
        Func<Task> act = () => _sut.UpdateBusAsync(4, request, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*upcoming schedules*");
    }

    [Fact]
    public async Task UpdateBusAsync_BusNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _busRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Bus?)null);

        var request = new UpdateBusRequest { BusType = "AC", TotalSeats = 10, OperatorName = "T" };

        // Act & Assert
        Func<Task> act = () => _sut.UpdateBusAsync(999, request, 1, "127.0.0.1");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteBusAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBusAsync_NoConflicts_SoftDeletesBus()
    {
        // Arrange
        var bus = TestDataBuilder.ActiveBus(id: 7);
        _busRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(bus);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForBusAsync(7)).ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasActiveBookingsForBusAsync(7)).ReturnsAsync(false);
        _busRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Bus>())).Returns(Task.CompletedTask);

        // Provide a valid HttpContext with a user claim
        var httpContext = new DefaultHttpContext();
        httpContext.User = TestHelpers.BuildClaimsPrincipal(userId: 1, role: "Admin");
        _httpAccessorMock.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await _sut.DeleteBusAsync(7);

        // Assert
        bus.IsDeleted.Should().BeTrue();
        _busRepoMock.Verify(r => r.UpdateAsync(bus), Times.Once);
    }

    [Fact]
    public async Task DeleteBusAsync_HasActiveBookings_ThrowsConflictException()
    {
        // Arrange
        var bus = TestDataBuilder.ActiveBus(id: 8);
        _busRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(bus);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForBusAsync(8)).ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasActiveBookingsForBusAsync(8)).ReturnsAsync(true);

        // Act & Assert
        Func<Task> act = () => _sut.DeleteBusAsync(8);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*active bookings*");
    }

    // ── GetByOperatorAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByOperatorAsync_ValidOperator_ReturnsMatchingBuses()
    {
        // Arrange
        var buses = new List<Bus> { TestDataBuilder.ActiveBus(1), TestDataBuilder.ActiveBus(2) };
        _busRepoMock
            .Setup(r => r.GetByOperatorAsync("Test Travels", 1, 10))
            .ReturnsAsync((buses, 2));

        // Act
        var (items, total) = await _sut.GetByOperatorAsync("Test Travels", 1, 10);

        // Assert
        items.Should().HaveCount(2);
        total.Should().Be(2);
        items.Should().AllSatisfy(b => b.OperatorName.Should().Be("Test Travels"));
    }

    [Fact]
    public async Task GetByOperatorAsync_EmptyOperatorName_ThrowsBadRequestException()
    {
        // Act & Assert
        Func<Task> act = () => _sut.GetByOperatorAsync("  ", 1, 10);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Operator name is required*");
    }
}
