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
            try
            {
                var result = await _sourceService.CreateSourceAsync(request);
                return Ok(ApiResponse<SourceResponseDto>.SuccessResponse("Source created successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAllSources([FromBody] PaginationRequest request)
        {
            try
            {
                var result = await _sourceService.GetAllSourcesAsync(request.PageNumber, request.PageSize);
                return Ok(ApiResponse<List<SourceResponseDto>>.SuccessResponse("Sources retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetSourceById(int id)
        {
            try
            {
                var result = await _sourceService.GetSourceByIdAsync(id);
                return Ok(ApiResponse<SourceResponseDto>.SuccessResponse("Source retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSource(int id, [FromBody] UpdateSourceRequest request)
        {
            try
            {
                await _sourceService.UpdateSourceAsync(id, request);
                return Ok(ApiResponse<string>.SuccessResponse("Source updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSource(int id)
        {
            try
            {
                await _sourceService.DeleteSourceAsync(id);
                return Ok(ApiResponse<string>.SuccessResponse("Source deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }
    }
}
