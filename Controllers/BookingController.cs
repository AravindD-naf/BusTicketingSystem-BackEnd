using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ISeatService _seatService;
        private readonly IPaymentService _paymentService;
        private readonly IPassengerService _passengerService;
        private readonly IScheduleService _scheduleService;
        private readonly IBusService _busService;

        public BookingController(
            IBookingService bookingService,
            ISeatService seatService,
            IPaymentService paymentService,
            IPassengerService passengerService,
            IScheduleService scheduleService,
            IBusService busService)
        {
            _bookingService = bookingService;
            _seatService = seatService;
            _paymentService = paymentService;
            _passengerService = passengerService;
            _scheduleService = scheduleService;
            _busService = busService;
        }

        #region Schedule Browsing

        [AllowAnonymous]
        [HttpPost("schedules/get-all")]
        public async Task<IActionResult> GetSchedules([FromBody] PaginationRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var result = await _scheduleService.GetAllAsync(request.PageNumber, request.PageSize);
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpPost("schedules/search")]
        public async Task<IActionResult> SearchSchedules([FromBody] ScheduleSearchRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 20;
            var result = await _scheduleService.SearchSchedulesAsync(request);
            return Ok(result);
        }

        #endregion

        #region Seat Management

        [Authorize]
        [HttpPost("seats/{scheduleId}")]
        public async Task<IActionResult> GetSeatLayout(int scheduleId)
        {
            var result = await _seatService.GetSeatLayoutAsync(scheduleId);
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("seats/lock")]
        public async Task<IActionResult> LockSeats([FromBody] LockSeatsRequestDto dto)
        {
            var result = await _seatService.LockSeatsAsync(
                dto.ScheduleId, dto.SeatNumbers, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("seats/release")]
        public async Task<IActionResult> ReleaseSeats([FromBody] ReleaseSeatsRequestDto dto)
        {
            var result = await _seatService.ReleaseSeatsAsync(
                dto.ScheduleId, dto.SeatNumbers, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        #endregion

        #region Booking

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequestDto dto)
        {
            var result = await _bookingService.CreateBookingAsync(dto, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("my-bookings")]
        public async Task<IActionResult> MyBookings([FromBody] PaginationRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var result = await _bookingService.GetMyBookingsAsync(GetUserId());
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("get-all")]
        public async Task<IActionResult> AllBookings([FromBody] PaginationRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var (items, totalCount) = await _bookingService
                .GetAllBookingsAsync(request.PageNumber, request.PageSize);
            return Ok(ApiResponse<object>.SuccessResponse("Bookings retrieved successfully", new
            {
                items,
                totalCount,
                pageNumber = request.PageNumber,
                pageSize = request.PageSize
            }));
        }

        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var result = await _bookingService.GetBookingByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "Admin,Customer")]
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
            var result = await _bookingService.CancelBookingAsync(id, GetUserId(), role, GetIpAddress());
            return Ok(result);
        }

        #endregion

        #region Payment

        [Authorize(Roles = "Customer")]
        [HttpPost("payment/initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestDto dto)
        {
            var result = await _paymentService.InitiatePaymentAsync(
                dto.BookingId, dto.Amount, dto.PaymentMethod, GetUserId(), GetIpAddress(), dto.PromoCode);
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("payment/confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto dto)
        {
            var result = await _paymentService.ConfirmPaymentAsync(dto, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("payment/{paymentId}")]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            var result = await _paymentService.GetPaymentAsync(paymentId);
            return Ok(result);
        }

        #endregion

        #region Rating

        [Authorize(Roles = "Customer")]
        [HttpPost("{bookingId}/rate")]
        public async Task<IActionResult> RateBus(int bookingId, [FromBody] RateBusRequestDto dto)
        {
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);
            if (booking?.Data == null)
                return NotFound(ApiResponse<string>.FailureResponse("Booking not found."));

            if (booking.Data.BookingStatus != "Confirmed")
                return BadRequest(ApiResponse<string>.FailureResponse("You can only rate a confirmed booking."));

            await _busService.RateBusAsync(
                booking.Data.BusId, GetUserId(), dto.Rating, GetIpAddress());

            return Ok(ApiResponse<string>.SuccessResponse("Rating submitted. Thank you!"));
        }

        #endregion

        #region Passengers

        [Authorize(Roles = "Customer")]
        [HttpPost("passengers")]
        public async Task<IActionResult> AddPassengers([FromBody] AddPassengerRequestDto dto)
        {
            var result = await _passengerService.AddPassengersAsync(dto, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{bookingId}/passengers")]
        public async Task<IActionResult> GetPassengers(int bookingId)
        {
            var result = await _passengerService.GetBookingPassengersAsync(bookingId);
            return Ok(result);
        }

        #endregion

        #region Refund

        [Authorize(Roles = "Admin")]
        [HttpPost("refund/confirm")]
        public async Task<IActionResult> ConfirmRefund([FromBody] ConfirmRefundRequestDto dto)
        {
            var result = await _paymentService.ConfirmRefundAsync(dto, GetUserId(), GetIpAddress());
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("refund/{refundId}")]
        public async Task<IActionResult> GetRefund(int refundId)
        {
            var result = await _paymentService.GetRefundAsync(refundId);
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{bookingId}/refund")]
        public async Task<IActionResult> GetRefundByBooking(int bookingId)
        {
            var result = await _paymentService.GetRefundByBookingIdAsync(bookingId);
            return Ok(result);
        }

        #endregion

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private string GetIpAddress() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}