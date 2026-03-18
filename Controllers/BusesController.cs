using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/buses")]
    public class BusesController : ControllerBase
    {
        private readonly IBusService _busService;

        public BusesController(IBusService busService)
        {
            _busService = busService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateBus(CreateBusRequest request)
        {
            //var result = await _busService.CreateBusAsync(request);

            //var userIdClaim = User.FindFirst("UserId");

            //if (userIdClaim == null)
            //    throw new UnauthorizedAccessException("Invalid token.");

            //var userId = int.Parse(userIdClaim.Value);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var result = await _busService.CreateBusAsync(request, userId, ipAddress);

            return Ok(ApiResponse<BusResponse>.SuccessResponse("Bus created successfully", result));
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAllBuses([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var result = await _busService.GetAllBusesAsync(request.PageNumber, request.PageSize);
                return Ok(ApiResponse<List<BusResponse>>.SuccessResponse("Buses retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetBus(int id)
        {
            try
            {
                var result = await _busService.GetBusByIdAsync(id);
                return Ok(ApiResponse<BusResponse>.SuccessResponse("Bus retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBus(int id, [FromBody] UpdateBusRequest request)
        {
            if (request == null)
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid request body."));

            var userIdClaim = User.FindFirst("UserId")
                              ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(ApiResponse<string>.FailureResponse("Invalid token."));

            var userId = int.Parse(userIdClaim.Value);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            await _busService.UpdateBusAsync(id, request, userId, ipAddress);

            return Ok(ApiResponse<string>.SuccessResponse("Bus updated successfully."));
        }
        

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBus(int id)
        {
            await _busService.DeleteBusAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Bus deleted successfully"));
        }


        [AllowAnonymous]
        [HttpPost("search-by-operator")]
        public async Task<IActionResult> GetByOperator([FromBody] OperatorSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OperatorName))
                    return BadRequest(ApiResponse<string>.FailureResponse("Operator name is required"));

                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var (buses, totalCount) = await _busService.GetByOperatorAsync(
                    request.OperatorName,
                    request.PageNumber,
                    request.PageSize);

                return Ok(ApiResponse<object>.SuccessResponse("Buses retrieved successfully", new
                {
                    totalCount,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    data = buses
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }
    }
}