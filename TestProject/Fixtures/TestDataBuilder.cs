using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;

namespace BusTicketingSystem.Tests.Fixtures;

public static class TestDataBuilder
{
    // ── Users ─────────────────────────────────────────────────────────────────

    public static User CustomerUser(int id = 2) => new()
    {
        UserId      = id,
        FullName    = "Test Customer",
        Email       = "customer@test.com",
        PhoneNumber = "9876543210",
        PasswordHash = string.Empty,
        RoleId      = 2,
        Role        = new Role { RoleId = 2, Name = "Customer" },
        IsActive    = true
    };

    // ── Buses ─────────────────────────────────────────────────────────────────

    public static Bus ActiveBus(int id = 1) => new()
    {
        BusId        = id,
        BusNumber    = $"TN01AB{1000 + id:D4}",
        BusType      = "AC Sleeper",
        TotalSeats   = 40,
        OperatorName = "Test Travels",
        IsActive     = true,
        IsDeleted    = false
    };

    public static Bus InactiveBus(int id = 1) => new()
    {
        BusId        = id,
        BusNumber    = $"TN02CD{2000 + id:D4}",
        BusType      = "Non-AC",
        TotalSeats   = 40,
        OperatorName = "Inactive Travels",
        IsActive     = false,
        IsDeleted    = false
    };

    // ── Routes ────────────────────────────────────────────────────────────────

    public static Models.Route ActiveRoute(int id = 1) => new()
    {
        RouteId                    = id,
        Source                     = "Chennai",
        Destination                = "Bangalore",
        Distance                   = 350,
        EstimatedTravelTimeMinutes = 360,
        BaseFare                   = 500,
        IsActive                   = true,
        IsDeleted                  = false
    };

    // ── Schedules ─────────────────────────────────────────────────────────────

    public static Schedule FutureSchedule(int id = 1, Bus? bus = null, Models.Route? route = null)
    {
        bus   ??= ActiveBus();
        route ??= ActiveRoute();
        return new Schedule
        {
            ScheduleId      = id,
            BusId           = bus.BusId,
            Bus             = bus,
            RouteId         = route.RouteId,
            Route           = route,
            TravelDate      = DateTime.UtcNow.AddDays(5),
            DepartureTime   = new TimeSpan(8, 0, 0),
            ArrivalTime     = new TimeSpan(14, 0, 0),
            Fare            = 600m,
            TotalSeats      = bus.TotalSeats,
            AvailableSeats  = bus.TotalSeats,
            IsActive        = true,
            IsDeleted       = false
        };
    }

    public static Schedule PastSchedule(int id = 99) => new()
    {
        ScheduleId      = id,
        BusId           = 1,
        Bus             = ActiveBus(),
        RouteId         = 1,
        Route           = ActiveRoute(),
        TravelDate      = DateTime.UtcNow.AddDays(-2),
        DepartureTime   = new TimeSpan(8, 0, 0),
        ArrivalTime     = new TimeSpan(14, 0, 0),
        Fare            = 600m,
        TotalSeats      = 40,
        AvailableSeats  = 40,
        IsActive        = true,
        IsDeleted       = false
    };

    // ── Seats ─────────────────────────────────────────────────────────────────

    public static Seat LockedSeat(int seatId, int scheduleId, int lockedByUserId, string seatNumber = "A1") => new()
    {
        SeatId          = seatId,
        ScheduleId      = scheduleId,
        SeatNumber      = seatNumber,
        SeatStatus      = "Locked",
        LockedByUserId  = lockedByUserId,
        LockedAt        = DateTime.UtcNow.AddMinutes(-2)
    };

    public static List<Seat> AvailableSeats(int scheduleId, int count) =>
        Enumerable.Range(1, count).Select(i => new Seat
        {
            SeatId     = i,
            ScheduleId = scheduleId,
            SeatNumber = $"A{i}",
            SeatStatus = "Available"
        }).ToList();

    // ── Bookings ──────────────────────────────────────────────────────────────

    public static Booking ConfirmedBooking(int id, int userId, int scheduleId = 1) => new()
    {
        BookingId     = id,
        UserId        = userId,
        ScheduleId    = scheduleId,
        NumberOfSeats = 1,
        TotalAmount   = 600m,
        BookingStatus = BookingStatus.Confirmed,
        BookingDate   = DateTime.UtcNow.AddDays(-1),
        PNR           = $"PNR{id:D6}"
    };

    public static Booking PendingBooking(int id, int userId, int scheduleId = 1) => new()
    {
        BookingId     = id,
        UserId        = userId,
        ScheduleId    = scheduleId,
        NumberOfSeats = 1,
        TotalAmount   = 600m,
        BookingStatus = BookingStatus.Pending,
        BookingDate   = DateTime.UtcNow,
        PNR           = $"PNR{id:D6}"
    };

    public static Booking CancelledBooking(int id, int userId, int scheduleId = 1) => new()
    {
        BookingId     = id,
        UserId        = userId,
        ScheduleId    = scheduleId,
        NumberOfSeats = 1,
        TotalAmount   = 600m,
        BookingStatus = BookingStatus.Cancelled,
        BookingDate   = DateTime.UtcNow.AddDays(-2),
        PNR           = $"PNR{id:D6}"
    };

    // ── Promo Codes ───────────────────────────────────────────────────────────

    public static PromoCode ActivePercentagePromo(int id = 1) => new()
    {
        PromoCodeId      = id,
        Code             = "TEST20",
        DiscountType     = DiscountType.Percentage,
        DiscountValue    = 20m,
        MaxDiscountAmount = 500m,
        MinBookingAmount = 300m,
        ValidFrom        = DateTime.UtcNow.AddDays(-10),
        ValidUntil       = DateTime.UtcNow.AddDays(30),
        IsActive         = true,
        CreatedAt        = DateTime.UtcNow
    };

    public static PromoCode ActiveFlatPromo(int id = 2) => new()
    {
        PromoCodeId      = id,
        Code             = "FLAT150",
        DiscountType     = DiscountType.Flat,
        DiscountValue    = 150m,
        MaxDiscountAmount = 150m,
        MinBookingAmount = 0m,
        ValidFrom        = DateTime.UtcNow.AddDays(-5),
        ValidUntil       = DateTime.UtcNow.AddDays(20),
        IsActive         = true,
        CreatedAt        = DateTime.UtcNow
    };

    public static PromoCode ExpiredPromo(int id = 3) => new()
    {
        PromoCodeId      = id,
        Code             = "EXPIRED",
        DiscountType     = DiscountType.Flat,
        DiscountValue    = 100m,
        MaxDiscountAmount = 100m,
        MinBookingAmount = 0m,
        ValidFrom        = DateTime.UtcNow.AddDays(-30),
        ValidUntil       = DateTime.UtcNow.AddDays(-1),
        IsActive         = true,
        CreatedAt        = DateTime.UtcNow
    };

    // ── Wallets ───────────────────────────────────────────────────────────────

    public static Wallet WalletWithBalance(int userId, decimal balance) => new()
    {
        WalletId  = userId,
        UserId    = userId,
        Balance   = balance,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
