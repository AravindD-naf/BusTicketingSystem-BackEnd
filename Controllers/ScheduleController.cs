using BusTicketingSystem.DTOs;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduleRequestDto dto)
        {
            var response = await _scheduleService.CreateAsync(dto, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAll([FromBody] PaginationRequest request)
        {
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1) request.PageSize = 10;
            var response = await _scheduleService.GetAllAsync(request.PageNumber, request.PageSize);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _scheduleService.GetByIdAsync(id);
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleRequestDto dto)
        {
            var response = await _scheduleService.UpdateAsync(id, dto, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _scheduleService.DeleteAsync(id, GetUserId(), GetIpAddress());
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search-by-from-city")]
        public async Task<IActionResult> GetByFromCity([FromBody] CitySearchRequest request)
        {
            var response = await _scheduleService.GetByFromCityAsync(request.City);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search-by-to-city")]
        public async Task<IActionResult> GetByToCity([FromBody] CitySearchRequest request)
        {
            var response = await _scheduleService.GetByToCityAsync(request.City);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] ScheduleSearchRequest request)
        {
            var response = await _scheduleService.SearchSchedulesAsync(
                request.FromCity, request.ToCity, request.TravelDateUtc);
            return Ok(response);
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private string GetIpAddress() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}