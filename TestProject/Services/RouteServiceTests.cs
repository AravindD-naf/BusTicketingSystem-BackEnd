using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using BusTicketingSystem.Services;
using BusTicketingSystem.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace BusTicketingSystem.Tests.Services;

public class RouteServiceTests
{
    private readonly Mock<IRouteRepository>    _routeRepoMock    = new();
    private readonly Mock<IAuditRepository>    _auditRepoMock    = new();
    private readonly Mock<IScheduleRepository> _scheduleRepoMock = new();
    private readonly RouteService              _sut;

    public RouteServiceTests()
    {
        _sut = new RouteService(
            _routeRepoMock.Object,
            _auditRepoMock.Object,
            _scheduleRepoMock.Object);

        // Default audit stubs
        _auditRepoMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(a => a.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    // ── CreateRouteAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRouteAsync_UniqueRoute_ReturnsCreatedDto()
    {
        // Arrange
        var request = new RouteCreateRequestDto
        {
            Source                     = "Chennai",
            Destination                = "Bangalore",
            Distance                   = 350,
            EstimatedTravelTimeMinutes = 360,
            BaseFare                   = 500
        };

        _routeRepoMock
            .Setup(r => r.GetBySourceDestinationAsync("Chennai", "Bangalore"))
            .ReturnsAsync((Models.Route?)null);

        _routeRepoMock.Setup(r => r.AddAsync(It.IsAny<Models.Route>())).Returns(Task.CompletedTask);
        _routeRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.CreateRouteAsync(request, userId: 1, ipAddress: "127.0.0.1");

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data!.Source.Should().Be("Chennai");
        response.Data.Destination.Should().Be("Bangalore");
        response.Data.BaseFare.Should().Be(500);
    }

    [Fact]
    public async Task CreateRouteAsync_InputIsTrimmed()
    {
        // Arrange
        var request = new RouteCreateRequestDto
        {
            Source = "  Madurai  ", Destination = "  Coimbatore  ",
            Distance = 200, EstimatedTravelTimeMinutes = 200, BaseFare = 300
        };

        _routeRepoMock
            .Setup(r => r.GetBySourceDestinationAsync("Madurai", "Coimbatore"))
            .ReturnsAsync((Models.Route?)null);

        _routeRepoMock.Setup(r => r.AddAsync(It.IsAny<Models.Route>())).Returns(Task.CompletedTask);
        _routeRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.CreateRouteAsync(request, 1, "127.0.0.1");

        // Assert: repository was called with trimmed values
        _routeRepoMock.Verify(r => r.GetBySourceDestinationAsync("Madurai", "Coimbatore"), Times.Once);
        response.Data!.Source.Should().Be("Madurai");
    }

    [Fact]
    public async Task CreateRouteAsync_DuplicateRoute_ThrowsConflictException()
    {
        // Arrange
        var existing = TestDataBuilder.ActiveRoute();
        _routeRepoMock
            .Setup(r => r.GetBySourceDestinationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(existing);

        var request = new RouteCreateRequestDto
        {
            Source = existing.Source, Destination = existing.Destination,
            Distance = 350, EstimatedTravelTimeMinutes = 360, BaseFare = 500
        };

        // Act & Assert
        Func<Task> act = () => _sut.CreateRouteAsync(request, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{existing.Source}*{existing.Destination}*already exists*");
    }

    // ── GetRouteByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRouteByIdAsync_ExistingRoute_ReturnsDto()
    {
        // Arrange
        var route = TestDataBuilder.ActiveRoute(id: 5);
        _routeRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(route);

        // Act
        var response = await _sut.GetRouteByIdAsync(5);

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.RouteId.Should().Be(5);
        response.Data.Source.Should().Be("Chennai");
    }

    [Fact]
    public async Task GetRouteByIdAsync_NonExistentRoute_ThrowsResourceNotFoundException()
    {
        // Arrange
        _routeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Models.Route?)null);

        // Act & Assert
        Func<Task> act = () => _sut.GetRouteByIdAsync(999);
        await act.Should().ThrowAsync<ResourceNotFoundException>()
            .WithMessage("*Route*not found*");
    }

    // ── UpdateRouteAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRouteAsync_NoFutureSchedules_UpdatesAndReturnsDto()
    {
        // Arrange
        var route = TestDataBuilder.ActiveRoute(id: 3);
        _routeRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(route);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForRouteAsync(3)).ReturnsAsync(false);
        _routeRepoMock.Setup(r => r.Update(It.IsAny<Models.Route>()));
        _routeRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var request = new RouteUpdateRequestDto
        {
            Source = "Trichy", Destination = "Madurai",
            Distance = 130, EstimatedTravelTimeMinutes = 150, BaseFare = 250, IsActive = true
        };

        // Act
        var response = await _sut.UpdateRouteAsync(3, request, 1, "127.0.0.1");

        // Assert
        response.Success.Should().BeTrue();
        route.Source.Should().Be("Trichy");
        route.Destination.Should().Be("Madurai");
        route.BaseFare.Should().Be(250);
    }

    [Fact]
    public async Task UpdateRouteAsync_HasFutureSchedules_ThrowsConflictException()
    {
        // Arrange
        var route = TestDataBuilder.ActiveRoute(id: 4);
        _routeRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(route);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForRouteAsync(4)).ReturnsAsync(true);

        var request = new RouteUpdateRequestDto
        {
            Source = "A", Destination = "B", Distance = 100,
            EstimatedTravelTimeMinutes = 100, BaseFare = 200
        };

        // Act & Assert
        Func<Task> act = () => _sut.UpdateRouteAsync(4, request, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*upcoming schedules*");
    }

    // ── DeleteRouteAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRouteAsync_NoConflicts_SoftDeletesRoute()
    {
        // Arrange
        var route = TestDataBuilder.ActiveRoute(id: 6);
        _routeRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(route);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForRouteAsync(6)).ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasActiveBookingsForRouteAsync(6)).ReturnsAsync(false);
        _routeRepoMock.Setup(r => r.Update(It.IsAny<Models.Route>()));
        _routeRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var response = await _sut.DeleteRouteAsync(6, 1, "127.0.0.1");

        // Assert
        response.Success.Should().BeTrue();
        route.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRouteAsync_HasActiveBookings_ThrowsConflictException()
    {
        // Arrange
        var route = TestDataBuilder.ActiveRoute(id: 7);
        _routeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(route);
        _scheduleRepoMock.Setup(r => r.HasFutureSchedulesForRouteAsync(7)).ReturnsAsync(false);
        _scheduleRepoMock.Setup(r => r.HasActiveBookingsForRouteAsync(7)).ReturnsAsync(true);

        // Act & Assert
        Func<Task> act = () => _sut.DeleteRouteAsync(7, 1, "127.0.0.1");
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*active bookings*");
    }

    // ── SearchRoutesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchRoutesAsync_BothParamsNull_ThrowsValidationException()
    {
        // Act & Assert
        Func<Task> act = () => _sut.SearchRoutesAsync(null, null, 1, 10);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*source or destination*");
    }

    [Fact]
    public async Task SearchRoutesAsync_ValidParams_ReturnsPaged()
    {
        // Arrange
        var routes = new List<Models.Route> { TestDataBuilder.ActiveRoute() };
        _routeRepoMock
            .Setup(r => r.SearchAsync("Chennai", null, 1, 10))
            .ReturnsAsync((routes, 1));

        // Act
        var response = await _sut.SearchRoutesAsync("Chennai", null, 1, 10);

        // Assert
        response.Success.Should().BeTrue();
        response.Data!.TotalCount.Should().Be(1);
        response.Data.Items.Should().HaveCount(1);
    }

    // ── GetAllRoutesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRoutesAsync_ReturnsCorrectPagedResponse()
    {
        // Arrange
        var routes = Enumerable.Range(1, 5)
            .Select(i => TestDataBuilder.ActiveRoute(i))
            .ToList();

        _routeRepoMock
            .Setup(r => r.GetPagedAsync(1, 10))
            .ReturnsAsync((routes, 5));

        // Act
        var response = await _sut.GetAllRoutesAsync(1, 10);

        // Assert
        response.Data!.Items.Should().HaveCount(5);
        response.Data.TotalCount.Should().Be(5);
        response.Data.PageNumber.Should().Be(1);
        response.Data.PageSize.Should().Be(10);
    }
}
