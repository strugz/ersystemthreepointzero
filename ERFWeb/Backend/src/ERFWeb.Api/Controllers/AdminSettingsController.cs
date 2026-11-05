using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>System Administrator console: workflow, business, and notification rules (flow 1, step 3).</summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminSettingsController(ISystemSettingsService settings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SystemSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken ct) => Ok(await settings.GetAsync(ct));

    [HttpPut]
    [ProducesResponseType<SystemSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemSettingsDto>> Update([FromBody] UpdateSystemSettingsRequest request, CancellationToken ct) =>
        Ok(await settings.UpdateAsync(request, ct));
}

/// <summary>
/// The R47 designer (Rules page): the layout both PDF engines print the R47 with, and a preview of an
/// unsaved layout on a real report the administrator may view.
/// </summary>
[ApiController]
[Route("api/admin/r47-design")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminR47DesignController(IR47DesignService designs, IExpenseReportPdfRenderer renderer, IClock clock) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<R47DesignDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<R47DesignDto>> Get(CancellationToken ct) => Ok(await designs.GetAsync(ct));

    [HttpPut]
    [ProducesResponseType<R47DesignDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<R47DesignDto>> Save([FromBody] R47Design design, CancellationToken ct) =>
        Ok(await designs.SaveAsync(design, ct));

    /// <summary>Reports to preview on (drafts included), by employee name, purpose or ER reference; only reports the caller may view.</summary>
    [HttpGet("reports")]
    [ProducesResponseType<IReadOnlyList<R47PreviewReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<R47PreviewReportDto>>> Reports([FromQuery] string? search, CancellationToken ct) =>
        Ok(await designs.SearchReportsAsync(search, ct));

    /// <summary>
    /// The R47 PDF drawn with the unsaved layout, of the sample report, a picked report, or a filed report by ER
    /// reference. Saves nothing.
    /// </summary>
    [HttpPost("preview")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview([FromBody] R47DesignPreviewRequest request, CancellationToken ct)
    {
        var document = await designs.PreviewDocumentAsync(request, ct);
        Response.Headers.ContentDisposition = "inline; filename=\"R47-preview.pdf\"";
        return File(renderer.Render(document, clock.UtcNow), "application/pdf");
    }
}

/// <summary>Daily SLA reminder job: on or off, next run and what the last run did (Phase 8.4).</summary>
[ApiController]
[Route("api/admin/reminders")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminRemindersController(IReminderStatusService reminders) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<ReminderStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReminderStatusDto>> Status(CancellationToken ct) => Ok(await reminders.GetAsync(ct));
}

/// <summary>Read-only audit trail: System Administrator and Finance (spec 9.10).</summary>
[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = Policies.AuditReader)]
[Produces("application/json")]
public sealed class AdminAuditController(IAdminAuditService auditTrail) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminAuditEntryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminAuditEntryDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] SortDirection direction = SortDirection.Desc,
        [FromQuery] string? @event = null,
        [FromQuery] long? actorUserId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] bool administrationOnly = true,
        CancellationToken ct = default)
    {
        var query = new AdminAuditQuery
        {
            Page = page,
            PageSize = pageSize,
            Direction = direction,
            Event = @event,
            ActorUserId = actorUserId,
            From = from,
            To = to,
            AdministrationOnly = administrationOnly,
        };
        return Ok(await auditTrail.ListAsync(query, ct));
    }
}
