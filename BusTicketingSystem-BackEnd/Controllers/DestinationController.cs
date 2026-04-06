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
            var result = await _destinationService.CreateDestinationAsync(request);
            return Ok(ApiResponse<DestinationResponseDto>.SuccessResponse("Destination created successfully", result));
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAllDestinations([FromBody] PaginationRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var (items, totalCount) = await _destinationService
                .GetAllDestinationsAsync(request.PageNumber, request.PageSize);
            return Ok(ApiResponse<object>.SuccessResponse("Destinations retrieved successfully", new
            {
                items,
                totalCount,
                pageNumber = request.PageNumber,
                pageSize = request.PageSize
            }));
        }

        [AllowAnonymous]
        [HttpGet("by-city/{cityName}")]
        public async Task<IActionResult> GetByCity(string cityName)
        {
            var result = await _destinationService.GetByCityAsync(cityName);
            return Ok(ApiResponse<List<DestinationResponseDto>>.SuccessResponse(
                "Destinations retrieved successfully", result));
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetDestinationById(int id)
        {
            var result = await _destinationService.GetDestinationByIdAsync(id);
            return Ok(ApiResponse<DestinationResponseDto>.SuccessResponse(
                "Destination retrieved successfully", result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDestination(int id, [FromBody] UpdateDestinationRequest request)
        {
            await _destinationService.UpdateDestinationAsync(id, request);
            return Ok(ApiResponse<string>.SuccessResponse("Destination updated successfully"));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDestination(int id)
        {
            await _destinationService.DeleteDestinationAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Destination deleted successfully"));
        }
    }
}