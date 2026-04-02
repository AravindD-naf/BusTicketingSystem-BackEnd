using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class BookingServiceTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────────
    private readonly Mock<IBookingRepository>  _bookingRepoMock  = new();
    private readonly Mock<IScheduleRepository> _scheduleRepoMock = new();
    private readonly Mock<ISeatRepository>     _seatRepoMock     = new();
    private readonly Mock<IAuditRepository>    _auditRepoMock    = new();
    private readonly Mock<IPaymentService>     _paymentMock      = new();
    private readonly Mock<ISeatService>        _seatServiceMock  = new();

    private BookingService CreateSut(ApplicationDbContext ctx) =>
        new BookingService(
            _bookingRepoMock.Object,
            _scheduleRepoMock.Object,
            _seatRepoMock.Object,
            _seatServiceMock.Object,
            _auditRepoMock.Object,
            _paymentMock.Object,
            ctx);

    public BookingServiceTests()
    {
        _auditRepoMock
            .Setup(a => a.LogAuditAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _bookingRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _seatRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _seatRepoMock.Setup(r => r.UpdateManyAsync(It.IsAny<List<Seat>>())).Returns(Task.CompletedTask);
    }

    // ── CreateBookingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_ReturnsBookingDto()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var schedule = TestDataBuilder.FutureSchedule(id: 1);
        var seat = TestDataBuilder.LockedSeat(seatId: 1, scheduleId: 1, lockedByUserId: 2, seatNumber: "A1");

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _seatRepoMock.Setup(r => r.GetSeatsByNumbersAsync(1, It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Seat> { seat });
        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

        var dto = new CreateBookingRequestDto
        {
            ScheduleId = 1,
            SeatNumbers = new List<string> { "A1" },
            ContactPhone = "9876543210",
            ContactEmail = "test@test.com"
        };

        // Act
        var result = await sut.CreateBookingAsync(dto, userId: 2, ipAddress: "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data!.ScheduleId.Should().Be(1);
        result.Data.NumberOfSeats.Should().Be(1);
        result.Data.TotalAmount.Should().Be(600m); // schedule.Fare = 600
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsProvided_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var dto = new CreateBookingRequestDto { ScheduleId = 1, SeatNumbers = new List<string>() };

        // Act & Assert
        Func<Task> act = () => sut.CreateBookingAsync(dto, 2, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*At least one seat*");
    }

    [Fact]
    public async Task CreateBookingAsync_MoreThanSixSeats_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var dto = new CreateBookingRequestDto
        {
            ScheduleId  = 1,
            SeatNumbers = Enumerable.Range(1, 7).Select(i => $"A{i}").ToList()
        };

        // Act & Assert
        Func<Task> act = () => sut.CreateBookingAsync(dto, 2, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Maximum 6 seats*");
    }

    [Fact]
    public async Task CreateBookingAsync_ScheduleNotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Schedule?)null);

        var dto = new CreateBookingRequestDto { ScheduleId = 999, SeatNumbers = new List<string> { "A1" } };

        // Act & Assert
        Func<Task> act = () => sut.CreateBookingAsync(dto, 2, "127.0.0.1");
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task CreateBookingAsync_PastSchedule_ThrowsBookingOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var pastSchedule = TestDataBuilder.PastSchedule();
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(pastSchedule);

        var dto = new CreateBookingRequestDto { ScheduleId = 99, SeatNumbers = new List<string> { "A1" } };

        // Act & Assert
        Func<Task> act = () => sut.CreateBookingAsync(dto, 2, "127.0.0.1");
        await act.Should().ThrowAsync<BookingOperationException>()
            .WithMessage("*departure time*");
    }

    [Fact]
    public async Task CreateBookingAsync_SeatLockedByAnotherUser_ThrowsSeatOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var schedule  = TestDataBuilder.FutureSchedule();
        var seat      = TestDataBuilder.LockedSeat(seatId: 5, scheduleId: 1, lockedByUserId: 99);

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _seatRepoMock.Setup(r => r.GetSeatsByNumbersAsync(1, It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Seat> { seat });

        var dto = new CreateBookingRequestDto { ScheduleId = 1, SeatNumbers = new List<string> { "A1" } };

        // Act & Assert — user 2 tries to book a seat locked by user 99
        Func<Task> act = () => sut.CreateBookingAsync(dto, userId: 2, ipAddress: "127.0.0.1");
        await act.Should().ThrowAsync<SeatOperationException>()
            .WithMessage("*locked by another user*");
    }

    [Fact]
    public async Task CreateBookingAsync_SeatNotLocked_ThrowsSeatOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        var schedule = TestDataBuilder.FutureSchedule();
        var seat     = TestDataBuilder.AvailableSeats(scheduleId: 1, count: 1).First();
        seat.SeatNumber = "A1";

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _seatRepoMock.Setup(r => r.GetSeatsByNumbersAsync(1, It.IsAny<List<string>>()))
                     .ReturnsAsync(new List<Seat> { seat });

        var dto = new CreateBookingRequestDto { ScheduleId = 1, SeatNumbers = new List<string> { "A1" } };

        // Act & Assert
        Func<Task> act = () => sut.CreateBookingAsync(dto, userId: 2, ipAddress: "127.0.0.1");
        await act.Should().ThrowAsync<SeatOperationException>()
            .WithMessage("*not locked*");
    }

    // ── RateBookingAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RateBookingAsync_OutOfRange_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        // Act & Assert — rating 6 is out of range
        Func<Task> act = () => sut.RateBookingAsync(bookingId: 1, userId: 2, rating: 6);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*between 1 and 5*");
    }

    [Fact]
    public async Task RateBookingAsync_ZeroRating_ThrowsValidationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var sut = CreateSut(ctx);

        Func<Task> act = () => sut.RateBookingAsync(1, 2, rating: 0);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RateBookingAsync_ConfirmedBooking_ValidRating_Succeeds()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();

        var bus      = TestDataBuilder.ActiveBus();
        var route    = TestDataBuilder.ActiveRoute();
        var schedule = TestDataBuilder.FutureSchedule(bus: bus, route: route);
        var booking  = TestDataBuilder.ConfirmedBooking(id: 10, userId: 2, scheduleId: 1);
        schedule.ScheduleId = 1;
        booking.ScheduleId = 1;

        ctx.Buses.Add(bus);
        ctx.Routes.Add(route);
        ctx.Schedules.Add(schedule);
        ctx.Bookings.Add(booking);
        ctx.SaveChanges();

        _bookingRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(booking);
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);

        var sut = CreateSut(ctx);

        // Act
        var result = await sut.RateBookingAsync(bookingId: 10, userId: 2, rating: 4);

        // Assert
        result.Success.Should().BeTrue();
        ctx.BusRatings.Should().ContainSingle(r => r.BookingId == 10 && r.Rating == 4);
    }

    [Fact]
    public async Task RateBookingAsync_PendingBooking_ThrowsBookingOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var booking = TestDataBuilder.PendingBooking(id: 20, userId: 2);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(booking);

        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.RateBookingAsync(20, 2, rating: 4);
        await act.Should().ThrowAsync<BookingOperationException>()
            .WithMessage("*confirmed bookings*");
    }

    [Fact]
    public async Task RateBookingAsync_UnauthorisedUser_ThrowsBookingOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var booking = TestDataBuilder.ConfirmedBooking(id: 30, userId: 2);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(booking);

        var sut = CreateSut(ctx);

        // Act & Assert — user 99 does not own booking 30
        Func<Task> act = () => sut.RateBookingAsync(30, userId: 99, rating: 3);
        await act.Should().ThrowAsync<BookingOperationException>()
            .WithMessage("*not authorized*");
    }

    // ── CancelBookingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ThrowsBookingOperationException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var booking = TestDataBuilder.CancelledBooking(id: 50, userId: 2);
        _bookingRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(booking);

        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.CancelBookingAsync(50, userId: 2, role: "Customer", ipAddress: "127.0.0.1");
        await act.Should().ThrowAsync<BookingOperationException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public async Task CancelBookingAsync_CustomerCancelsOtherPersonsBooking_Throws()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        var booking = TestDataBuilder.PendingBooking(id: 60, userId: 5);   // owned by user 5
        _bookingRepoMock.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(booking);

        var sut = CreateSut(ctx);

        // Act & Assert — user 2 tries to cancel booking owned by user 5
        Func<Task> act = () => sut.CancelBookingAsync(60, userId: 2, role: "Customer", ipAddress: "127.0.0.1");
        await act.Should().ThrowAsync<BookingOperationException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task CancelBookingAsync_NotFoundBooking_ThrowsResourceNotFoundException()
    {
        // Arrange
        await using var ctx = DbContextFactory.CreateInMemory();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Booking?)null);

        var sut = CreateSut(ctx);

        // Act & Assert
        Func<Task> act = () => sut.CancelBookingAsync(999, 2, "Customer", "127.0.0.1");
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
