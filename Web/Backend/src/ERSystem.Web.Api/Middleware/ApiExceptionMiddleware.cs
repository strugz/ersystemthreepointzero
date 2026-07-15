using ERSystem.Web.Application.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace ERSystem.Web.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title, code) = exception switch
            {
                AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "antiforgery_validation_failed"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", (string?)null),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", (string?)null),
                NotFoundException => (StatusCodes.Status404NotFound, "Not found", (string?)null),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", (string?)null),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", (string?)null)
            };
            if (status >= 500) logger.LogError(exception, "Unhandled API exception");
            else logger.LogWarning("API request failed with {Status}: {Message}", status, exception.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500 ? "Contact support with the correlation ID." : exception.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = context.TraceIdentifier;
            if (code is not null) problem.Extensions["code"] = code;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
