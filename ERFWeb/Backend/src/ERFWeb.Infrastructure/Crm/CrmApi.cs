using System.Net.Http.Json;
using System.Text.Json;
using ERFWeb.Application.Common;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Crm;

/// <summary>
/// The request plumbing the CRM clients share: the server-held API key in a header, and every failure
/// turned into a <see cref="ServiceUnavailableException"/>. Logs only the operation name and the HTTP
/// status or exception type: never the URL (it carries names or IDs), the key, or the response body.
/// </summary>
internal sealed class CrmApi(HttpClient http, CrmOptions options, ILogger logger, string operation, string notConfigured, string unavailable)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsConfigured => options.IsConfigured;

    public async Task<HttpResponseMessage> SendAsync(string relativePath, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new ServiceUnavailableException(notConfigured, "crm_not_configured");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{options.BaseUrl!.TrimEnd('/')}/{relativePath}");
        request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");

        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Only the exception type is logged; its message can carry the request URL.
            logger.LogWarning("The CRM did not answer {Operation} ({Error}).", operation, ex.GetType().Name);
            throw new ServiceUnavailableException(unavailable, "crm_unavailable");
        }
    }

    public void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // 401 (wrong key), 503 (key not set on the CRM), 5xx: nothing the user can fix from the screen.
        logger.LogWarning("The CRM refused {Operation} (HTTP {Status}).", operation, (int)response.StatusCode);
        throw new ServiceUnavailableException(unavailable, "crm_unavailable");
    }

    public async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
        where T : class
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, ct)
                ?? throw new JsonException("Empty body.");
        }
        catch (JsonException)
        {
            logger.LogWarning("The CRM answered {Operation} with a body ERFWeb could not read (HTTP {Status}).", operation, (int)response.StatusCode);
            throw new ServiceUnavailableException(unavailable, "crm_unavailable");
        }
    }
}
