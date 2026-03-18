using System.Net;
using System.Text.Json;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Services;
using Microsoft.EntityFrameworkCore;

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

      
        private async Task HandleExceptionAsync(HttpContext context, Exception exception, IErrorLogService errorLogService)
        {
            context.Response.ContentType = "application/json";

            int statusCode = StatusCodes.Status500InternalServerError;
            string errorCode = "UNKNOWN_ERROR";
            string userMessage = "An unexpected error occurred";
            string internalMessage = exception.Message;
            Dictionary<string, string> errors = new();
            string traceId = context.TraceIdentifier;

            try
            {
                int? userId = null;
                if (context.User?.FindFirst("UserId")?.Value != null)
                {
                    int.TryParse(context.User.FindFirst("UserId").Value, out int uid);
                    userId = uid;
                }

                if (exception is Exceptions.ApplicationException appEx)
                {
                    statusCode = appEx.StatusCode;
                    errorCode = appEx.ErrorCode;
                    userMessage = appEx.UserMessage;
                    internalMessage = appEx.InternalMessage;
                    errors = appEx.Errors;
                }
                else if (exception is Exceptions.NotFoundException)
                {
                    statusCode = StatusCodes.Status404NotFound;
                    errorCode = "NOT_FOUND";
                    userMessage = exception.Message;
                }
                else if (exception is Exceptions.BadRequestException)
                {
                    statusCode = StatusCodes.Status400BadRequest;
                    errorCode = "BAD_REQUEST";
                    userMessage = exception.Message;
                }
                else if (exception is Exceptions.ObjectAlreadyExistsException)
                {
                    statusCode = StatusCodes.Status409Conflict;
                    errorCode = "ALREADY_EXISTS";
                    userMessage = exception.Message;
                }
                else if (exception is DbUpdateConcurrencyException)
                {
                    statusCode = StatusCodes.Status409Conflict;
                    errorCode = "CONCURRENCY_VIOLATION";
                    userMessage = "The resource has been modified. Please refresh and try again.";
                    internalMessage = "Optimistic concurrency violation occurred";
                }
                else if (exception is DbUpdateException dbEx)
                {
                    statusCode = StatusCodes.Status500InternalServerError;
                    errorCode = "DATABASE_ERROR";
                    userMessage = "An error occurred while saving data";
                    internalMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                }
                else if (exception is OperationCanceledException)
                {
                    statusCode = StatusCodes.Status408RequestTimeout;
                    errorCode = "REQUEST_CANCELLED";
                    userMessage = "The request was cancelled. Please try again.";
                }

                await errorLogService.LogErrorAsync(exception, context, userId);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log exception to database");
            }

            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                message = userMessage,
                errorCode = errorCode,
                statusCode = statusCode,
                errors = errors.Count > 0 ? errors : null,
                timestamp = DateTime.UtcNow,
                traceId = traceId
            };

            if (statusCode >= 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception: {ErrorCode} - {Message}",
                    errorCode,
                    userMessage);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    exception,
                    "Client error: {ErrorCode} - {Message}",
                    errorCode,
                    userMessage);
            }

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}