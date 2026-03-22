using System.Text.Json;
using BusTicketingSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusTicketingSystem.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, errorLogService);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            IErrorLogService errorLogService)
        {
            // Must be set before writing body
            context.Response.ContentType = "application/json";

            int statusCode;
            string errorCode;
            string userMessage;
            Dictionary<string, string> errors = new();
            string traceId = context.TraceIdentifier;

            // ── Resolve user ID from JWT claims (NameIdentifier, not "UserId") ──
            int? userId = null;
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int uid))
                userId = uid;

            // ── Map exception to HTTP status + error code + user-facing message ──
            switch (exception)
            {
                // ── Our typed ApplicationException hierarchy (covers all CustomExceptions) ──
                case Exceptions.ApplicationException appEx:
                    statusCode = appEx.StatusCode;
                    errorCode = appEx.ErrorCode;
                    userMessage = appEx.UserMessage;
                    errors = appEx.Errors;
                    break;

                // ── Legacy thin exceptions still in the codebase ──
                case Exceptions.NotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = "NOT_FOUND";
                    userMessage = exception.Message;
                    break;

                case Exceptions.BadRequestException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "BAD_REQUEST";
                    userMessage = exception.Message;
                    break;

                // ── BookingExceptions.cs hierarchy ──
                case Exceptions.BookingNotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = "BOOKING_NOT_FOUND";
                    userMessage = exception.Message;
                    break;

                case Exceptions.InvalidSeatStatusException:
                    statusCode = StatusCodes.Status409Conflict;
                    errorCode = "INVALID_SEAT_STATUS";
                    userMessage = exception.Message;
                    break;

                case Exceptions.SeatNotFoundException:
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = "SEAT_NOT_FOUND";
                    userMessage = exception.Message;
                    break;

                case Exceptions.InsufficientSeatsException:
                    statusCode = StatusCodes.Status409Conflict;
                    errorCode = "INSUFFICIENT_SEATS";
                    userMessage = exception.Message;
                    break;

                case Exceptions.InvalidScheduleException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "INVALID_SCHEDULE";
                    userMessage = exception.Message;
                    break;

                case Exceptions.InvalidPassengerException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "INVALID_PASSENGER";
                    userMessage = exception.Message;
                    break;

                case Exceptions.BookingCancellationException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "CANCELLATION_ERROR";
                    userMessage = exception.Message;
                    break;

                case Exceptions.PaymentTimeoutException:
                    statusCode = StatusCodes.Status408RequestTimeout;
                    errorCode = "PAYMENT_TIMEOUT";
                    userMessage = exception.Message;
                    break;

                case Exceptions.PaymentException:
                    statusCode = StatusCodes.Status402PaymentRequired;
                    errorCode = "PAYMENT_ERROR";
                    userMessage = exception.Message;
                    break;

                case Exceptions.RefundException:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "REFUND_ERROR";
                    userMessage = exception.Message;
                    break;

                // ── System unauthorized (thrown by services before we migrated) ──
                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status403Forbidden;
                    errorCode = "FORBIDDEN";
                    userMessage = exception.Message;
                    break;

                // ── Null argument (e.g. AuthService null request guard) ──
                case ArgumentNullException argEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "BAD_REQUEST";
                    userMessage = $"Required parameter '{argEx.ParamName}' was not provided.";
                    break;

                // ── Not implemented (e.g. UpdatePassengerAsync stub) ──
                case NotImplementedException:
                    statusCode = StatusCodes.Status501NotImplemented;
                    errorCode = "NOT_IMPLEMENTED";
                    userMessage = "This feature is not yet available.";
                    break;

                // ── EF Core concurrency ──
                case DbUpdateConcurrencyException:
                    statusCode = StatusCodes.Status409Conflict;
                    errorCode = "CONCURRENCY_VIOLATION";
                    userMessage = "The resource was modified by another request. Please refresh and try again.";
                    break;

                // ── EF Core DB write failures (unique constraint etc.) ──
                case DbUpdateException dbEx:
                    statusCode = StatusCodes.Status500InternalServerError;
                    errorCode = "DATABASE_ERROR";
                    userMessage = "An error occurred while saving data. Please try again.";
                    // Log the inner message for diagnostics but never send it to the client
                    _logger.LogError(dbEx, "DbUpdateException: {Inner}", dbEx.InnerException?.Message ?? dbEx.Message);
                    break;

                // ── Request cancellation (client disconnected) ──
                case OperationCanceledException:
                    statusCode = StatusCodes.Status408RequestTimeout;
                    errorCode = "REQUEST_CANCELLED";
                    userMessage = "The request was cancelled. Please try again.";
                    break;

                // ── Absolute fallback ──
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    errorCode = "INTERNAL_SERVER_ERROR";
                    userMessage = "An unexpected error occurred. Please try again later.";
                    break;
            }

            // ── Log to DB (non-blocking — failure here must not hide the real error) ──
            try
            {
                await errorLogService.LogErrorAsync(exception, context, userId);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to persist error log to database");
            }

            // ── Write structured log ──
            if (statusCode >= 500)
                _logger.LogError(exception, "[{ErrorCode}] {Message} | TraceId: {TraceId}", errorCode, userMessage, traceId);
            else
                _logger.LogWarning(exception, "[{ErrorCode}] {Message} | TraceId: {TraceId}", errorCode, userMessage, traceId);

            // ── Write response ──
            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                message = userMessage,
                errorCode = errorCode,
                statusCode = statusCode,
                errors = errors.Count > 0 ? errors : null,
                traceId = traceId,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}