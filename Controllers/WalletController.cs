using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/wallet")]
    [Authorize(Roles = "Customer")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// GET /api/v1/wallet — get balance + last 50 transactions
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var result = await _walletService.GetOrCreateWalletAsync(GetUserId());
            return Ok(ApiResponse<object>.SuccessResponse("Wallet retrieved", result));
        }

        /// POST /api/v1/wallet/topup — add money
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] WalletTopUpRequest request)
        {
            var result = await _walletService.TopUpAsync(
                GetUserId(), request.Amount, request.PaymentMethod, GetIpAddress());
            return Ok(ApiResponse<object>.SuccessResponse("Wallet topped up successfully", result));
        }

        /// POST /api/v1/wallet/debit — internal use (called by payment flow)
        [HttpPost("debit")]
        public async Task<IActionResult> Debit([FromBody] WalletDebitRequest request)
        {
            var result = await _walletService.DebitAsync(
                GetUserId(), request.Amount, request.Description, request.ReferenceId, GetIpAddress());
            return Ok(ApiResponse<object>.SuccessResponse("Wallet debited", result));
        }

        /// POST /api/v1/wallet/credit — internal use (called by refund flow)
        [HttpPost("credit")]
        public async Task<IActionResult> Credit([FromBody] WalletCreditRequest request)
        {
            var result = await _walletService.CreditAsync(
                GetUserId(), request.Amount, request.Description, request.ReferenceId, GetIpAddress());
            return Ok(ApiResponse<object>.SuccessResponse("Wallet credited", result));
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private string GetIpAddress() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
