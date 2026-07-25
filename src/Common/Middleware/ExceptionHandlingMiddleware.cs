using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Middleware
{
    /// <summary>
    /// Thrown by service-layer code for expected business rule violations
    /// (e.g. insufficient stock, inactive product, invalid state transition).
    /// Mapped to 400 Bad Request by the exception middleware.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>Thrown when a requested entity does not exist. Mapped to 404.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>Thrown when the caller is authenticated but not allowed to perform the action. Mapped to 403.</summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }

    /// <summary>
    /// Catches all unhandled exceptions, logs them, and returns a consistent
    /// ProblemDetails-shaped JSON response instead of leaking stack traces.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                BusinessRuleException => (HttpStatusCode.BadRequest, "Business Rule Violation"),
                ForbiddenException => (HttpStatusCode.Forbidden, "Forbidden"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
            }
            else
            {
                _logger.LogWarning("{Title} on {Path}: {Message}", title, context.Request.Path, exception.Message);
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}",
                title,
                status = (int)statusCode,
                detail = statusCode == HttpStatusCode.InternalServerError
                    ? "An unexpected error occurred. Please contact support if the problem persists."
                    : exception.Message,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
