using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Middleware;

/// <summary>Validates the antiforgery token on all unsafe (state-changing) requests.</summary>
public sealed class AntiforgeryMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    public async Task Invoke(HttpContext context)
    {
        if (RequiresValidation(context.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Antiforgery validation failed.",
                    Instance = context.Request.Path,
                };
                problem.Extensions["code"] = "antiforgery_validation_failed";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem, problem.GetType());
                return;
            }
        }

        await next(context);
    }

    private static bool RequiresValidation(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}
