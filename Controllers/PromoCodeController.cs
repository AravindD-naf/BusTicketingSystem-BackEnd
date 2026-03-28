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
        public PromoCodeController(IPromoCodeService promoService) => _promoService = promoService;

        // Public — used on landing page to show active promos
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var result = await _promoService.GetActiveAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [Authorize]
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidatePromoCodeRequest request)
        {
            var result = await _promoService.ValidateAsync(request.Code, request.BookingAmount);
            return Ok(ApiResponse<object>.SuccessResponse("Promo code validated", result));
        }

        // Admin CRUD
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _promoService.GetAllAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromoCodeRequest request)
        {
            var result = await _promoService.CreateAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse("Promo code created", result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePromoCodeRequest request)
        {
            var result = await _promoService.UpdateAsync(id, request);
            return Ok(ApiResponse<object>.SuccessResponse("Promo code updated", result));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _promoService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Promo code deleted"));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var result = await _promoService.ToggleActiveAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Status toggled", result));
        }
    }
}
