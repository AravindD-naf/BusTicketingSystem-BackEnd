using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BusTicketingSystem.Data;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Services
{
   
    public interface IErrorLogService
    {
        Task LogErrorAsync(
            Exception exception,
            HttpContext context,
            int? userId = null);

        Task<List<ErrorLog>> GetErrorsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string errorCode = null,
            bool? isCritical = null);

        Task<ErrorLog> GetErrorAsync(int errorLogId);

        Task MarkAsResolvedAsync(int errorLogId, string notes);

        Task DeleteOldErrorsAsync(int daysToKeep = 30);
    }

    public class ErrorLogService : IErrorLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ErrorLogService> _logger;

        public ErrorLogService(
            ApplicationDbContext context,
            ILogger<ErrorLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

      
        public async Task LogErrorAsync(
            Exception exception,
            HttpContext context,
            int? userId = null)
        {
            try
            {
                var errorLog = new ErrorLog();

                if (exception is Exceptions.ApplicationException appEx)
                {
                    errorLog.ExceptionType = exception.GetType().Name;
                    errorLog.ErrorCode = appEx.ErrorCode;
                    errorLog.UserMessage = appEx.UserMessage;
                    errorLog.InternalMessage = appEx.InternalMessage;
                    errorLog.StatusCode = appEx.StatusCode;
                    errorLog.ValidationErrors = JsonSerializer.Serialize(appEx.Errors);
                    errorLog.ContextData = JsonSerializer.Serialize(appEx.ContextData);
                    errorLog.Severity = GetSeverityLevel(appEx.StatusCode);
                    errorLog.IsCritical = appEx.StatusCode >= 500;
                }
                else
                {
                    errorLog.ExceptionType = exception.GetType().Name;
                    errorLog.ErrorCode = "UNKNOWN_ERROR";
                    errorLog.UserMessage = "An unexpected error occurred";
                    errorLog.InternalMessage = exception.Message;
                    errorLog.StatusCode = 500;
                    errorLog.Severity = "Error";
                    errorLog.IsCritical = true;
                }

                errorLog.StackTrace = exception.StackTrace;
                errorLog.InnerExceptionMessage = exception.InnerException?.Message;
                errorLog.UserId = userId;

                if (context != null)
                {
                    errorLog.RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
                    errorLog.HttpMethod = context.Request.Method;
                    errorLog.ClientIpAddress = context.Connection.RemoteIpAddress?.ToString();
                    errorLog.TraceId = context.TraceIdentifier;

                    try
                    {
                        if (context.Request.HasFormContentType)
                        {
                            var form = await context.Request.ReadFormAsync();
                            errorLog.RequestBody = JsonSerializer.Serialize(form.ToDictionary(x => x.Key, x => x.Value.ToString()));
                        }
                    }
                    catch
                    {
                        // Ignore form reading errors
                    }

                    try
                    {
                        var headers = new Dictionary<string, string>();
                        foreach (var header in context.Request.Headers)
                        {
                            headers[header.Key] = header.Value.ToString();
                        }
                        errorLog.RequestHeaders = JsonSerializer.Serialize(headers);
                    }
                    catch
                    {
                        // Ignore header reading errors
                    }
                }

                errorLog.IsHandled = true;
                errorLog.CreatedAt = DateTime.UtcNow;

                await _context.ErrorLogs.AddAsync(errorLog);
                await _context.SaveChangesAsync();

                LogToApplicationLogger(errorLog, exception);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log error to database");
                _logger.LogError(exception, "Original exception that failed to be logged");
            }
        }

 
        public async Task<List<ErrorLog>> GetErrorsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string errorCode = null,
            bool? isCritical = null)
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (!string.IsNullOrEmpty(errorCode))
                query = query.Where(e => e.ErrorCode.Contains(errorCode));

            if (isCritical.HasValue)
                query = query.Where(e => e.IsCritical == isCritical.Value);

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

  
        public async Task<ErrorLog> GetErrorAsync(int errorLogId)
        {
            return await _context.ErrorLogs.FindAsync(errorLogId);
        }


        public async Task MarkAsResolvedAsync(int errorLogId, string notes)
        {
            var errorLog = await _context.ErrorLogs.FindAsync(errorLogId);
            if (errorLog != null)
            {
                errorLog.ResolvedAt = DateTime.UtcNow;
                errorLog.ResolutionNotes = notes;
                _context.ErrorLogs.Update(errorLog);
                await _context.SaveChangesAsync();
            }
        }

  
        public async Task DeleteOldErrorsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldErrors = await _context.ErrorLogs
                .Where(e => e.CreatedAt < cutoffDate && e.ResolvedAt != null)
                .ToListAsync();

            if (oldErrors.Count > 0)
            {
                _context.ErrorLogs.RemoveRange(oldErrors);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deleted {oldErrors.Count} resolved error logs older than {daysToKeep} days");
            }
        }

        private string GetSeverityLevel(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => "Critical",
                >= 400 => "Warning",
                _ => "Info"
            };
        }

        private void LogToApplicationLogger(ErrorLog errorLog, Exception exception)
        {
            if (errorLog.IsCritical)
            {
                _logger.LogCritical(
                    exception,
                    "Critical error: {ErrorCode} - {Message}",
                    errorLog.ErrorCode,
                    errorLog.UserMessage);
            }
            else if (errorLog.Severity == "Warning")
            {
                _logger.LogWarning(
                    exception,
                    "Warning: {ErrorCode} - {Message}",
                    errorLog.ErrorCode,
                    errorLog.UserMessage);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "Error: {ErrorCode} - {Message}",
                    errorLog.ErrorCode,
                    errorLog.UserMessage);
            }
        }
    }
}
