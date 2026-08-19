using Microsoft.AspNetCore.Antiforgery;

namespace ERSystem.Web.Api.Middleware;

public sealed class AntiforgeryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.Request.Path.StartsWithSegments("/api") && !SafeMethods.Contains(context.Request.Method))
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        await next(context);
    }
}
