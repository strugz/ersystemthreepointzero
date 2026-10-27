using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Notifications;
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
