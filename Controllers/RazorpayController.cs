using BusTicketingSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public RazorpayController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// POST /api/v1/razorpay/create-order
        /// Creates a Razorpay order and returns order_id + key_id to the frontend
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            var keyId     = _config["Razorpay:KeyId"]     ?? throw new InvalidOperationException("Razorpay KeyId missing");
            var keySecret = _config["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret missing");

            // Razorpay amount is in paise (1 INR = 100 paise)
            var amountInPaise = (int)(req.Amount * 100);

            var orderPayload = new
            {
                amount   = amountInPaise,
                currency = "INR",
                receipt  = $"booking_{req.BookingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
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
                amount   = amountInPaise,
                currency = "INR",
                bookingId = req.BookingId
            }));
        }

        /// POST /api/v1/razorpay/verify
        /// Verifies the Razorpay payment signature
        [HttpPost("verify")]
        public IActionResult Verify([FromBody] VerifyPaymentRequest req)
        {
            var keySecret = _config["Razorpay:KeySecret"] ?? "";

            // HMAC-SHA256 signature verification
            var payload   = $"{req.RazorpayOrderId}|{req.RazorpayPaymentId}";
            var keyBytes  = Encoding.UTF8.GetBytes(keySecret);
            var msgBytes  = Encoding.UTF8.GetBytes(payload);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(msgBytes);
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

            var isValid = computed == req.RazorpaySignature?.ToLower();

            return Ok(ApiResponse<object>.SuccessResponse(new { isValid }));
        }
    }

    public class CreateOrderRequest
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
    }

    public class VerifyPaymentRequest
    {
        public string RazorpayOrderId   { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
