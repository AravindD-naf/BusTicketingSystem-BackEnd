using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/destinations")]
    public class DestinationController : ControllerBase
    {
        private readonly DestinationService _destinationService;

        public DestinationController(DestinationService destinationService)
        {
            _destinationService = destinationService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateDestination([FromBody] CreateDestinationRequest request)
        {
            try
            {
                var result = await _destinationService.CreateDestinationAsync(request);
                return Ok(ApiResponse<DestinationResponseDto>.SuccessResponse("Destination created successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAllDestinations([FromBody] PaginationRequest request)
        {
            try
            {
                var result = await _destinationService.GetAllDestinationsAsync(request.PageNumber, request.PageSize);
                return Ok(ApiResponse<List<DestinationResponseDto>>.SuccessResponse("Destinations retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetDestinationById(int id)
        {
            try
            {
                var result = await _destinationService.GetDestinationByIdAsync(id);
                return Ok(ApiResponse<DestinationResponseDto>.SuccessResponse("Destination retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDestination(int id, [FromBody] UpdateDestinationRequest request)
        {
            try
            {
                await _destinationService.UpdateDestinationAsync(id, request);
                return Ok(ApiResponse<string>.SuccessResponse("Destination updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDestination(int id)
        {
            try
            {
                await _destinationService.DeleteDestinationAsync(id);
                return Ok(ApiResponse<string>.SuccessResponse("Destination deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }
    }
}
