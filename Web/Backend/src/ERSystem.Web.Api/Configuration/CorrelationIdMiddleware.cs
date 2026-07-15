namespace ERSystem.Web.Api.Configuration;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = Guid.TryParse(supplied, out var parsed) ? parsed : Guid.NewGuid();
        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId.ToString();
        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>().BeginScope(
                   new Dictionary<string, object> { [ItemName] = correlationId }))
        {
            await next(context);
        }
    }

    public static Guid Get(HttpContext context) =>
        context.Items.TryGetValue(ItemName, out var value) && value is Guid id ? id : Guid.NewGuid();
}
