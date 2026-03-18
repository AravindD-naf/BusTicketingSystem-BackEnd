using Asp.Versioning;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/routes")]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAll([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var response = await _routeService.GetAllRoutesAsync(request.PageNumber, request.PageSize);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var response = await _routeService.GetRouteByIdAsync(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(RouteCreateRequestDto request)
        {
            var userId = GetUserId();
            var ipAddress = GetIpAddress();

            var response = await _routeService
                .CreateRouteAsync(request, userId, ipAddress);

            return Ok(response);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, RouteUpdateRequestDto request)
        {
            var userId = GetUserId();
            var ipAddress = GetIpAddress();

            var response = await _routeService
                .UpdateRouteAsync(id, request, userId, ipAddress);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var ipAddress = GetIpAddress();

            var response = await _routeService
                .DeleteRouteAsync(id, userId, ipAddress);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search-by-source")]
        public async Task<IActionResult> GetBySource([FromBody] RouteSourceSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Source))
                    return BadRequest(ApiResponse<string>.FailureResponse("Source is required"));

                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var response = await _routeService.GetRoutesBySourceAsync(
                    request.Source,
                    request.PageNumber,
                    request.PageSize);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("search-by-destination")]
        public async Task<IActionResult> GetByDestination([FromBody] RouteDestinationSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Destination))
                    return BadRequest(ApiResponse<string>.FailureResponse("Destination is required"));

                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var response = await _routeService.GetRoutesByDestinationAsync(
                    request.Destination,
                    request.PageNumber,
                    request.PageSize);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] RouteAdvancedSearchRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var response = await _routeService.SearchRoutesAsync(
                    request.Source,
                    request.Destination,
                    request.PageNumber,
                    request.PageSize);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }


        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return claim != null ? int.Parse(claim) : 0;
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}