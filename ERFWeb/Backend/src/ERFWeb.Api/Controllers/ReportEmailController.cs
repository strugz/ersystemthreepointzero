using ERFWeb.Api.Middleware;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Email the R47 PDF of a report, and read its email sent status. Anyone who may view the report may
/// email it; the server picks the recipients (claimant, Level 1 and Level 2 signatories, and the sender),
/// so this endpoint cannot be pointed at an arbitrary address. Outcomes come back as 200 or ProblemDetails
/// with a friendly <c>title</c> and a stable <c>code</c>.
/// </summary>
[ApiController]
[Route("api/reports/{reportId:guid}/email")]
[Authorize]
[Produces("application/json")]
public sealed class ReportEmailController(IReportAccessPolicy access, IReportEmailService email) : ControllerBase
{
    /// <summary>
    /// Email the R47. Send the same <c>idempotencyKey</c> when retrying: an email already sent under it is
    /// not sent again (200 with <c>replayed: true</c>).
    /// </summary>
    [HttpPost]
    [ProducesResponseType<EmailSentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Send(Guid reportId, [FromBody] SendReportEmailRequest? request, CancellationToken ct)
    {
        await EnsureCanViewAsync(reportId, ct);
        var result = await email.SendR47Async(reportId, request ?? new SendReportEmailRequest(null), ct);
        return result.Outcome switch
        {
            EmailSendOutcome.Sent => Ok(new EmailSentDto("Sent", result.RecipientCount, result.Replayed)),
            EmailSendOutcome.InvalidRequest => Failure(StatusCodes.Status400BadRequest, "validation_failed",
                "Please check the request and try again.", result.Errors),
            EmailSendOutcome.ReportNotFound => Failure(StatusCodes.Status404NotFound, "email_report_not_found",
                "That expense report could not be found."),
            EmailSendOutcome.ReportNotFiled => Failure(StatusCodes.Status422UnprocessableEntity, "email_report_not_filed",
                "File the report before emailing it."),
            EmailSendOutcome.NoRecipients => Failure(StatusCodes.Status422UnprocessableEntity, "email_no_recipients",
                "No one to email has an email address on file, so nothing was sent."),
            EmailSendOutcome.NotConfigured => Failure(StatusCodes.Status503ServiceUnavailable, "email_not_configured",
                "Email is not set up on the server yet. Please contact the system administrator."),
            EmailSendOutcome.GatewayFailed => Failure(StatusCodes.Status502BadGateway, "email_gateway_failed",
                "The mail server could not send the email. Please try again later."),
            EmailSendOutcome.GatewayUnconfirmed => Failure(StatusCodes.Status502BadGateway, "email_gateway_unconfirmed",
                "The mail server did not confirm the email. It may still arrive, so it was not sent again."),
            EmailSendOutcome.InProgress => Failure(StatusCodes.Status409Conflict, "email_in_progress",
                "This email is already being sent."),
            EmailSendOutcome.KeyReused => Failure(StatusCodes.Status409Conflict, "email_idempotency_key_reused",
                "This request key was already used for a different email. Start a new send."),
            _ => Failure(StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred."),
        };
    }

    /// <summary>The latest emails about the report, newest first (manual and workflow), without addresses or text.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReportEmailDeliveryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IReadOnlyList<ReportEmailDeliveryDto>> History(Guid reportId, CancellationToken ct)
    {
        await EnsureCanViewAsync(reportId, ct);
        return await email.HistoryAsync(reportId, ct);
    }

    private async Task EnsureCanViewAsync(Guid reportId, CancellationToken ct)
    {
        if (!await access.CanViewAsync(reportId, ct))
        {
            throw new ForbiddenException("You are not allowed to view this report.");
        }
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
