using ERFWeb.Api.Middleware;
using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Notification SMS through the Lasius gateway. The client names the recipient by user id only; the
/// server texts the numbers on file, so this endpoint cannot be pointed at an arbitrary phone. Every
/// outcome comes back as a normal response: 200 with the result, or ProblemDetails with a friendly
/// <c>title</c> and a stable <c>code</c>. The message text is never echoed back.
/// </summary>
[ApiController]
[Route("api/sms")]
[Produces("application/json")]
public sealed class SmsController(ISmsService sms) : ControllerBase
{
    /// <summary>
    /// Text a user's text group. Send the same <c>idempotencyKey</c> when retrying: a message already sent
    /// under it is not sent again (200 with <c>replayed: true</c>). With <c>reportId</c>, the report's Level 1
    /// and Level 2 signatories who have a mobile number are texted along with the claimant.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.SmsSender)]
    [ProducesResponseType<SmsSentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Send([FromBody] SendSmsRequest request, CancellationToken ct)
    {
        var result = await sms.SendAsync(request, ct);
        return result.Outcome switch
        {
            SmsSendOutcome.Sent => Ok(new SmsSentDto("Sent", result.RecipientCount, result.Replayed)),
            SmsSendOutcome.InvalidRequest => Failure(StatusCodes.Status400BadRequest, "validation_failed",
                "Please check the SMS details and try again.", result.Errors),
            SmsSendOutcome.UserNotFound => Failure(StatusCodes.Status404NotFound, "sms_user_not_found",
                "That user could not be found or is no longer active."),
            SmsSendOutcome.ReportNotFound => Failure(StatusCodes.Status404NotFound, "sms_report_not_found",
                "That expense report could not be found."),
            SmsSendOutcome.NoTextGroup => Failure(StatusCodes.Status422UnprocessableEntity, "sms_no_text_group",
                "No one to text has a mobile number on file, so no SMS was sent."),
            SmsSendOutcome.NotConfigured => Failure(StatusCodes.Status503ServiceUnavailable, "sms_not_configured",
                "SMS sending is not set up on the server yet. Please contact the system administrator."),
            SmsSendOutcome.GatewayFailed => Failure(StatusCodes.Status502BadGateway, "sms_gateway_failed",
                "The SMS service could not send the message. Please try again later."),
            SmsSendOutcome.GatewayUnconfirmed => Failure(StatusCodes.Status502BadGateway, "sms_gateway_unconfirmed",
                "The SMS service did not confirm the message. It may still arrive, so it was not sent again."),
            SmsSendOutcome.InProgress => Failure(StatusCodes.Status409Conflict, "sms_in_progress",
                "This SMS is already being sent."),
            SmsSendOutcome.KeyReused => Failure(StatusCodes.Status409Conflict, "sms_idempotency_key_reused",
                "This request key was already used for a different message. Start a new send."),
            _ => Failure(StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred."),
        };
    }

    /// <summary>Same ProblemDetails shape the exception middleware produces, so the SPA reads every error alike.</summary>
    private ObjectResult Failure(int status, string code, string title, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Instance = HttpContext.Request.Path };
        problem.Extensions["code"] = code;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
