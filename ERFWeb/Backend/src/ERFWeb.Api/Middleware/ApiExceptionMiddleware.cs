using ERFWeb.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Middleware;

/// <summary>
/// Translates expected <see cref="AppException"/>s (and unexpected errors) into
/// RFC 7807 ProblemDetails responses with a stable machine <c>code</c>.
/// </summary>
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var problem = Build(context, StatusCodes.Status400BadRequest, "Validation failed.", ex.Code);
            problem.Extensions["errors"] = ex.Errors;
            await WriteAsync(context, problem);
        }
        catch (AppException ex)
        {
            var status = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ConflictException => StatusCodes.Status409Conflict,
                ServiceUnavailableException => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest,
            };
            await WriteAsync(context, Build(context, status, ex.Message, ex.Code));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, Build(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "internal_error"));
        }
    }

    private static ProblemDetails Build(HttpContext context, int status, string title, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path,
        };
        problem.Extensions["code"] = code;
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        return problem;
    }

    private static async Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }
}
