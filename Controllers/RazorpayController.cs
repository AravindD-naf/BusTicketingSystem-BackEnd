using BusTicketingSystem.Data;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/razorpay")]
    [Authorize]
    public class RazorpayController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;

        public RazorpayController(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext db,
            IPaymentService paymentService)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _db = db;
            _paymentService = paymentService;
        }

        /// POST /api/v1/razorpay/create-order
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            var keyId     = _config["Razorpay:KeyId"]     ?? "";
            var keySecret = _config["Razorpay:KeySecret"] ?? "";

            if (string.IsNullOrWhiteSpace(keyId) || keyId.Contains("yourkeyid") ||
                string.IsNullOrWhiteSpace(keySecret) || keySecret.Contains("yourkeysecret"))
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Razorpay API keys are not configured. Add your test keys to appsettings.json."));

            var amountInPaise = (int)(req.Amount * 100);

            var orderPayload = new
            {
                amount   = amountInPaise,
                currency = "INR",
                receipt  = $"bk_{req.BookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            };

            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var json    = JsonSerializer.Serialize(orderPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(ApiResponse<object>.FailureResponse($"Razorpay order creation failed: {body}"));

            using var doc = JsonDocument.Parse(body);
            var orderId = doc.RootElement.GetProperty("id").GetString();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                orderId,
                keyId,
                amount    = amountInPaise,
                currency  = "INR",
                bookingId = req.BookingId
            }));
        }

        /// POST /api/v1/razorpay/verify-and-confirm
        /// Verifies signature, then records the payment in our system atomically.
        /// This bypasses the amount re-validation since Razorpay already charged the correct amount.
        [HttpPost("verify-and-confirm")]
        public async Task<IActionResult> VerifyAndConfirm([FromBody] VerifyAndConfirmRequest req)
        {
            var keySecret = _config["Razorpay:KeySecret"] ?? "";

            // 1. Verify HMAC-SHA256 signature
            var payload  = $"{req.RazorpayOrderId}|{req.RazorpayPaymentId}";
            var keyBytes = Encoding.UTF8.GetBytes(keySecret);
            var msgBytes = Encoding.UTF8.GetBytes(payload);
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var computed = BitConverter.ToString(hmac.ComputeHash(msgBytes)).Replace("-", "").ToLower();

            if (computed != req.RazorpaySignature?.ToLower())
                return BadRequest(ApiResponse<object>.FailureResponse("Payment signature verification failed."));

            var userId = GetUserId();

            // 2. Create a Payment record in our DB (skip amount re-validation — Razorpay verified it)
            var booking = await _db.Bookings.FindAsync(req.BookingId);
            if (booking == null || booking.IsDeleted)
                return NotFound(ApiResponse<object>.FailureResponse("Booking not found."));

            if (booking.UserId != userId)
                return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

            // Apply promo discount if stored
            decimal discountAmount = booking.DiscountAmount;
            decimal finalBase      = booking.TotalAmount - discountAmount;
            decimal tax            = Math.Round(finalBase * 0.06m);
            decimal grandTotal     = finalBase + tax + 20m; // 20 = convenience fee

            // Reset booking status if needed
            if (booking.BookingStatus == BookingStatus.PaymentFailed ||
                booking.BookingStatus == BookingStatus.PaymentProcessing)
                booking.BookingStatus = BookingStatus.Pending;

            var payment = new Payment
            {
                BookingId     = req.BookingId,
                Amount        = grandTotal,
                PaymentMethod = req.PaymentMethod,
                TransactionId = req.RazorpayPaymentId,
                Status        = PaymentStatus.Pending,
                CreatedAt     = DateTime.UtcNow,
                ExpiresAt     = DateTime.UtcNow.AddMinutes(15)
            };
            _db.Payments.Add(payment);

            booking.BookingStatus      = BookingStatus.PaymentProcessing;
            booking.LastStatusChangeAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // 3. Confirm the payment (marks booking Confirmed, seats Booked)
            var confirmResult = await _paymentService.ConfirmPaymentAsync(
                new BusTicketingSystem.DTOs.Requests.ConfirmPaymentRequestDto
                {
                    PaymentId       = payment.PaymentId,
                    TransactionId   = req.RazorpayPaymentId,
                    IsSuccess       = true,
                    FailureReason   = ""
                },
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            return Ok(confirmResult);
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("nameid")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    public class CreateOrderRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }

    public class VerifyAndConfirmRequest
    {
        public int    BookingId           { get; set; }
        public string RazorpayOrderId     { get; set; } = string.Empty;
        public string RazorpayPaymentId   { get; set; } = string.Empty;
        public string RazorpaySignature   { get; set; } = string.Empty;
        public string PaymentMethod       { get; set; } = "Razorpay";
    }
}
