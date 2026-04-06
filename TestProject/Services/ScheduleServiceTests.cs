using BusTicketingSystem.DTOs;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IScheduleRepository> _scheduleRepoMock = new();
    private readonly Mock<IRouteRepository>    _routeRepoMock    = new();
    private readonly Mock<IBusRepository>      _busRepoMock      = new();
    private readonly Mock<IAuditRepository>    _auditRepoMock    = new();
    private readonly Mock<ISeatRepository>     _seatRepoMock     = new();
    private readonly ScheduleService           _sut;

    public ScheduleServiceTests()
    {
        _sut = new ScheduleService(
            _scheduleRepoMock.Object,
            _routeRepoMock.Object,
            _busRepoMock.Object,
            _auditRepoMock.Object,
            _seatRepoMock.Object);

        // Default stubs
        _auditRepoMock.Setup(a => a.LogAuditAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _scheduleRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _seatRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<List<Seat>>())).Returns(Task.CompletedTask);
        _seatRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _busRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Bus>())).Returns(Task.CompletedTask);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsScheduleDto()
    {
        // Arrange
        var bus   = TestDataBuilder.ActiveBus(id: 1);
        var route = TestDataBuilder.ActiveRoute(id: 1);

        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(route);
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bus);
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bus);  // called twice (initial + activate)
        _scheduleRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                         .ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>(), It.IsAny<bool>()))
            .ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.AddAsync(It.IsAny<Schedule>())).Returns(Task.CompletedTask);

        var dto = new ScheduleRequestDto
        {
            BusId         = 1,
            RouteId       = 1,
            TravelDate    = DateTime.UtcNow.AddDays(5),
            DepartureTime = "08:00:00",
            ArrivalTime   = "14:00:00",
            Fare          = 550m
        };

        // Act
        var result = await _sut.CreateAsync(dto, userId: 1, ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.RouteId.Should().Be(1);
        result.Data.BusId.Should().Be(1);
        result.Data.BaseFare.Should().Be(550m);

        // Verify seats were generated (40 seats for the bus)
        _seatRepoMock.Verify(r => r.AddRangeAsync(It.Is<List<Seat>>(
            seats => seats.Count == 40)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ArrivalEquelsDeparture_ThrowsValidationException()
    {
        // Arrange — both times are the same
        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveRoute());
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveBus());

        var dto = new ScheduleRequestDto
        {
            BusId = 1, RouteId = 1,
            TravelDate    = DateTime.UtcNow.AddDays(3),
            DepartureTime = "10:00:00",
            ArrivalTime   = "10:00:00"
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateAsync(dto, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*arrivalTime*");
    }

    [Fact]
    public async Task CreateAsync_PastTravelDate_ThrowsValidationException()
    {
        // Arrange
        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveRoute());
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveBus());

        var dto = new ScheduleRequestDto
        {
            BusId = 1, RouteId = 1,
            TravelDate    = DateTime.UtcNow.AddDays(-1),
            DepartureTime = "08:00:00",
            ArrivalTime   = "14:00:00"
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateAsync(dto, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*travelDate*");
    }

    [Fact]
    public async Task CreateAsync_RouteInactive_ThrowsResourceNotFoundException()
    {
        // Arrange
        var inactiveRoute = TestDataBuilder.ActiveRoute();
        inactiveRoute.IsActive = false;
        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inactiveRoute);

        var dto = new ScheduleRequestDto
        {
            BusId = 1, RouteId = 1,
            TravelDate    = DateTime.UtcNow.AddDays(3),
            DepartureTime = "08:00:00",
            ArrivalTime   = "14:00:00"
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateAsync(dto, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_BusInactive_ThrowsResourceNotFoundException()
    {
        // Arrange
        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveRoute());
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.InactiveBus(id: 1));

        var dto = new ScheduleRequestDto
        {
            BusId = 1, RouteId = 1,
            TravelDate    = DateTime.UtcNow.AddDays(3),
            DepartureTime = "08:00:00",
            ArrivalTime   = "14:00:00"
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateAsync(dto, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateScheduleExact_ThrowsConflictException()
    {
        // Arrange
        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveRoute());
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.ActiveBus());
        _scheduleRepoMock
            .Setup(r => r.ExistsAsync(1, It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var dto = new ScheduleRequestDto
        {
            BusId = 1, RouteId = 1,
            TravelDate    = DateTime.UtcNow.AddDays(3),
            DepartureTime = "08:00:00",
            ArrivalTime   = "14:00:00"
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateAsync(dto, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already scheduled*");
    }

    [Fact]
    public async Task CreateAsync_OvernightJourney_SetsIsOvernightTrue()
    {
        // Arrange
        var bus   = TestDataBuilder.ActiveBus(id: 1);
        var route = TestDataBuilder.ActiveRoute(id: 1);
        bus.TotalSeats = 4;

        _routeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(route);
        _busRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bus);
        _scheduleRepoMock.Setup(r => r.ExistsAsync(1, It.IsAny<DateTime>(), It.IsAny<TimeSpan>()))
                         .ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasOverlappingScheduleAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>(), It.IsAny<bool>()))
            .ReturnsAsync(false);

        Schedule? capturedSchedule = null;
        _scheduleRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => capturedSchedule = s)
            .Returns(Task.CompletedTask);

        var dto = new ScheduleRequestDto
        {
            BusId         = 1,
            RouteId       = 1,
            TravelDate    = DateTime.UtcNow.AddDays(5),
            DepartureTime = "22:00:00",
            ArrivalTime   = "1.02:00:00",   // 26h in d.hh:mm:ss format → overnight
            Fare          = 700m
        };

        // Act
        await _sut.CreateAsync(dto, 1, "127.0.0.1");

        // Assert
        capturedSchedule!.IsOvernightArrival.Should().BeTrue();
        capturedSchedule.ArrivalTime.Should().Be(new TimeSpan(2, 0, 0), "wrapped to 02:00");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingSchedule_ReturnsDto()
    {
        // Arrange
        var schedule = TestDataBuilder.FutureSchedule(id: 5);
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(schedule);

        // Act
        var result = await _sut.GetByIdAsync(5);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.ScheduleId.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsResourceNotFoundException()
    {
        // Arrange
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Schedule?)null);

        // Act & Assert
        Func<Task> act = () => _sut.GetByIdAsync(999);
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingSchedule_SoftDeletesAndReturnsTrue()
    {
        // Arrange
        var schedule = TestDataBuilder.FutureSchedule(id: 7);
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(schedule);
        _scheduleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Schedule>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteAsync(7, userId: 1, ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        schedule.IsDeleted.Should().BeTrue();
        schedule.IsActive.Should().BeFalse();
    }

    // ── DurationMinutes computed correctly ────────────────────────────────────

    [Theory]
    [InlineData("08:00:00", "14:00:00", false, 360)]
    [InlineData("22:00:00", "02:00:00", true,  240)]   // 2h past midnight = 4h journey
    public async Task ScheduleDto_DurationMinutes_IsCorrect(
        string dep, string arr, bool overnight, int expectedMinutes)
    {
        // Arrange
        var schedule = TestDataBuilder.FutureSchedule();
        schedule.DepartureTime     = TimeSpan.Parse(dep);
        schedule.ArrivalTime       = TimeSpan.Parse(arr);
        schedule.IsOvernightArrival = overnight;
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Data!.DurationMinutes.Should().Be(expectedMinutes);
    }
}
