using System.Text.RegularExpressions;

namespace ERFWeb.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation id, echoed on the response and attached to every log line
/// written while the request runs, so an id a user reports can be found in the log files.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Log property name; the file log template prints it.</summary>
    public const string LogProperty = "CorrelationId";

    public async Task Invoke(HttpContext context)
    {
        // A caller-supplied id is kept only when it is short and plain, so it cannot inject text into the logs.
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value) && SafeId().IsMatch(value.ToString())
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { [LogProperty] = correlationId }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SafeId();
}
