using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/promo")]
    public class PromoCodeController : ControllerBase
    {
        private readonly IPromoCodeService _promoService;

        public PromoCodeController(IPromoCodeService promoService)
        {
            _promoService = promoService;
        }

        [Authorize]
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidatePromoCodeRequest request)
        {
            var result = await _promoService.ValidateAsync(request.Code, request.BookingAmount);
            return Ok(ApiResponse<object>.SuccessResponse("Promo code validated", result));
        }
    }
}
