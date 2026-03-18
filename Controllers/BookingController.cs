using BusTicketingSystem.DTOs;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
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

        public BookingController(
            IBookingService bookingService,
            ISeatService seatService,
            IPaymentService paymentService,
            IPassengerService passengerService,
            IScheduleService scheduleService)
        {
            _bookingService = bookingService;
            _seatService = seatService;
            _paymentService = paymentService;
            _passengerService = passengerService;
            _scheduleService = scheduleService;
        }

        #region Schedule Browsing Endpoints

        [AllowAnonymous]
        [HttpPost("schedules/get-all")]
        public async Task<IActionResult> GetSchedules([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var result = await _scheduleService.GetAllAsync(request.PageNumber, request.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("schedules/search")]
        public async Task<IActionResult> SearchSchedules([FromBody] ScheduleSearchRequest request)
        {
            try
            {
                var result = await _scheduleService
                    .SearchSchedulesAsync(request.FromCity, request.ToCity, request.TravelDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion

        #region Seat Management Endpoints

        [Authorize]
        [HttpPost("seats/{scheduleId}")]
        public async Task<IActionResult> GetSeatLayout(int scheduleId)
        {
            try
            {
                var result = await _seatService.GetSeatLayoutAsync(scheduleId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }


        /// Lock selected seats for 5 minutes
        [Authorize(Roles = "Customer")]
        [HttpPost("seats/lock")]
        public async Task<IActionResult> LockSeats([FromBody] LockSeatsRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _seatService.LockSeatsAsync(
                    dto.ScheduleId,
                    dto.SeatNumbers,
                    userId,
                    ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }


        [Authorize(Roles = "Customer")]
        [HttpPost("seats/release")]
        public async Task<IActionResult> ReleaseSeats([FromBody] ReleaseSeatsRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _seatService.ReleaseSeatsAsync(
                    dto.ScheduleId,
                    dto.SeatNumbers,
                    userId,
                    ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion

        #region Booking Endpoints

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                return Ok(await _bookingService
                    .CreateBookingAsync(dto, userId, ip));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }


        [Authorize(Roles = "Customer")]
        [HttpPost("my-bookings")]
        public async Task<IActionResult> MyBookings([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);

                return Ok(await _bookingService
                    .GetMyBookingsAsync(userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("get-all")]
        public async Task<IActionResult> AllBookings([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                return Ok(await _bookingService
                    .GetAllBookingsAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Admin,Customer")]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            try
            {
                return Ok(await _bookingService
                    .GetBookingByIdAsync(id));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Admin,Customer")]
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);

                string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                return Ok(await _bookingService
                    .CancelBookingAsync(id, userId, role, ip));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion

        #region Payment Endpoints

        [Authorize(Roles = "Customer")]
        [HttpPost("payment/initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _paymentService.InitiatePaymentAsync(
                    dto.BookingId,
                    dto.Amount,
                    dto.PaymentMethod,
                    userId,
                    ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("payment/confirm")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _paymentService.ConfirmPaymentAsync(dto, userId, ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("payment/{paymentId}")]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            try
            {
                var result = await _paymentService.GetPaymentAsync(paymentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion

        #region Passenger Endpoints

        [Authorize(Roles = "Customer")]
        [HttpPost("passengers")]
        public async Task<IActionResult> AddPassengers([FromBody] AddPassengerRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _passengerService.AddPassengersAsync(dto, userId, ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }


        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{bookingId}/passengers")]
        public async Task<IActionResult> GetPassengers(int bookingId)
        {
            try
            {
                var result = await _passengerService.GetBookingPassengersAsync(bookingId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion

        #region Refund Endpoints

        [Authorize(Roles = "Admin")]
        [HttpPost("refund/confirm")]
        public async Task<IActionResult> ConfirmRefund([FromBody] ConfirmRefundRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid token. UserId claim missing.",
                        Data = null
                    });
                }

                int userId = int.Parse(userIdClaim.Value);
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

                var result = await _paymentService.ConfirmRefundAsync(dto, userId, ip);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }


        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("refund/{refundId}")]
        public async Task<IActionResult> GetRefund(int refundId)
        {
            try
            {
                var result = await _paymentService.GetRefundAsync(refundId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        #endregion
    }
}
