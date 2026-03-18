//using BusTicketingSystem.DTOs;
//using BusTicketingSystem.Interfaces.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//[ApiController]
//[Route("api/v1/[controller]")]
//[Authorize(Roles = "Admin")]
//public class ScheduleController : ControllerBase
//{
//    private readonly IScheduleService _scheduleService;

//    public ScheduleController(IScheduleService scheduleService)
//    {
//        _scheduleService = scheduleService;
//    }

//    [HttpPost]
//    public async Task<IActionResult> Create(ScheduleRequestDto dto)
//    {
//        var response = await _scheduleService
//            .CreateAsync(dto, 1, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

//        return Ok(response);
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
//    {
//        var response = await _scheduleService
//            .GetAllAsync(pageNumber, pageSize);

//        return Ok(response);
//    }

//    [HttpGet("{id}")]
//    public async Task<IActionResult> GetById(int id)
//    {
//        var response = await _scheduleService.GetByIdAsync(id);
//        return Ok(response);
//    }
//}







using BusTicketingSystem.DTOs;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var response = await _scheduleService.CreateAsync(
                dto,
                1,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("get-all")]
        public async Task<IActionResult> GetAll([FromBody] PaginationRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var response = await _scheduleService.GetAllAsync(request.PageNumber, request.PageSize);
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
                var response = await _scheduleService.GetByIdAsync(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ScheduleRequestDto dto)
        {
            var response = await _scheduleService.UpdateAsync(
                id,
                dto,
                1,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _scheduleService.DeleteAsync(
                id,
                1,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            return Ok(response);
        }


        [AllowAnonymous]
        [HttpPost("search-by-from-city")]
        public async Task<IActionResult> GetByFromCity([FromBody] CitySearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.City))
                    return BadRequest(ApiResponse<string>.FailureResponse("City is required"));

                var response = await _scheduleService.GetByFromCityAsync(request.City);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("search-by-to-city")]
        public async Task<IActionResult> GetByToCity([FromBody] CitySearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.City))
                    return BadRequest(ApiResponse<string>.FailureResponse("City is required"));

                var response = await _scheduleService.GetByToCityAsync(request.City);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] ScheduleSearchRequest request)
        {
            try
            {
                var response = await _scheduleService.SearchSchedulesAsync(
                    request.FromCity,
                    request.ToCity,
                    request.TravelDate);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }
    }
}