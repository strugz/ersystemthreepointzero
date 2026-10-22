namespace ERFWeb.Api.Middleware;

/// <summary>
/// Browser-hardening headers on every API response. Kept to what cannot break the SPA on another origin:
/// no CSP <c>default-src</c> (it would stop the browser's PDF viewer on /reports/{id}/pdf) and no
/// Cross-Origin-Resource-Policy (the SPA on Render loads signature images from here). HSTS is left to nginx,
/// which serves other applications on the same host.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IReadOnlyList<string> frameAncestors)
{
    private readonly string _contentSecurityPolicy = "frame-ancestors 'self'" + string.Concat(frameAncestors.Select(o => " " + o));

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers.ContentSecurityPolicy = _contentSecurityPolicy;
            // Reports, receipts and signatures are personal data: no shared or browser cache keeps a copy.
            if (string.IsNullOrEmpty(headers.CacheControl))
            {
                headers.CacheControl = "no-store";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
