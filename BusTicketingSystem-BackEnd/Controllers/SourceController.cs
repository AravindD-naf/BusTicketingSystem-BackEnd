using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/sources")]
    public class SourceController : ControllerBase
    {
        private readonly SourceService _sourceService;

        public SourceController(SourceService sourceService)
        {
            _sourceService = sourceService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateSource([FromBody] CreateSourceRequest request)
        {
            var result = await _sourceService.CreateSourceAsync(request);
            return Ok(ApiResponse<SourceResponseDto>.SuccessResponse("Source created successfully", result));
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAllSources([FromBody] PaginationRequest request)
        {
            var (items, totalCount) = await _sourceService
                .GetAllSourcesAsync(request.PageNumber, request.PageSize);
            return Ok(ApiResponse<object>.SuccessResponse("Sources retrieved successfully", new
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
            var result = await _sourceService.GetByCityAsync(cityName);
            return Ok(ApiResponse<List<SourceResponseDto>>.SuccessResponse(
                "Sources retrieved successfully", result));
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetSourceById(int id)
        {
            var result = await _sourceService.GetSourceByIdAsync(id);
            return Ok(ApiResponse<SourceResponseDto>.SuccessResponse("Source retrieved successfully", result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSource(int id, [FromBody] UpdateSourceRequest request)
        {
            await _sourceService.UpdateSourceAsync(id, request);
            return Ok(ApiResponse<string>.SuccessResponse("Source updated successfully"));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSource(int id)
        {
            await _sourceService.DeleteSourceAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Source deleted successfully"));
        }
    }
}