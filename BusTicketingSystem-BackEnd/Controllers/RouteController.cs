using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/routes")]
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
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var response = await _routeService.GetAllRoutesAsync(request.PageNumber, request.PageSize);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _routeService.GetRouteByIdAsync(id);
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(RouteCreateRequestDto request)
        {
            var response = await _routeService.CreateRouteAsync(request, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, RouteUpdateRequestDto request)
        {
            var response = await _routeService.UpdateRouteAsync(id, request, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _routeService.DeleteRouteAsync(id, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search-by-source")]
        public async Task<IActionResult> GetBySource([FromBody] RouteSourceSearchRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var response = await _routeService.GetRoutesBySourceAsync(
                request.Source, request.PageNumber, request.PageSize);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search-by-destination")]
        public async Task<IActionResult> GetByDestination([FromBody] RouteDestinationSearchRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var response = await _routeService.GetRoutesByDestinationAsync(
                request.Destination, request.PageNumber, request.PageSize);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] RouteAdvancedSearchRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var response = await _routeService.SearchRoutesAsync(
                request.Source, request.Destination, request.PageNumber, request.PageSize);
            return Ok(response);
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private string GetIpAddress() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}