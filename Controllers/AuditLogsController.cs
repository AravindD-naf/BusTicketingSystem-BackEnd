using Asp.Versioning;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusTicketingSystem.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/auditlogs")]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpPost("get-all")]
        public async Task<IActionResult> GetLogs([FromBody] AuditLogSearchRequest request)
        {
            try
            {
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var (logs, totalCount) = await _auditService.GetPagedLogsAsync(
                    request.PageNumber, 
                    request.PageSize, 
                    request.EntityName, 
                    request.UserId, 
                    request.FromDate, 
                    request.ToDate);

                return Ok(ApiResponse<object>.SuccessResponse("Audit logs retrieved successfully", new
                {
                    totalCount,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    data = logs
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse(ex.Message));
            }
        }
    }
}
