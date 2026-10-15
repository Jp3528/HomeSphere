using HomeSphere.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdHeaderName]?.ToString() 
            ?? httpContext.TraceIdentifier;

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not Found", notFound.Message),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Conflict", conflict.Message),
            BusinessRuleValidationException validation => (StatusCodes.Status400BadRequest, "Business Rule Violation", validation.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Access denied."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred. Please contact support.")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "[{CorrelationId}] Unhandled exception occurred: {Message}", correlationId, exception.Message);
        }
        else
        {
            _logger.LogWarning("[{CorrelationId}] Handled domain exception ({StatusCode}): {Message}", correlationId, statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
