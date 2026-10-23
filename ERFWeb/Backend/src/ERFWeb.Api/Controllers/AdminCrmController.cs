using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// CRM employee lookup for the System Administrator's Add User and edit screens: the admin picks the CRM
/// person and the account stores that <c>employeeId</c>. The server calls the CRM; the browser never does.
/// 503 (<c>crm_not_configured</c> / <c>crm_unavailable</c>) when the CRM cannot answer.
/// </summary>
[ApiController]
[Route("api/admin/crm/employees")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminCrmController(ICrmEmployeeDirectory crm) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<CrmEmployeeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResult<CrmEmployeeDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await crm.SearchAsync(new CrmEmployeeSearchQuery { Search = search, IncludeInactive = includeInactive, Page = page, PageSize = pageSize }, ct));

    [HttpGet("{employeeId}")]
    [ProducesResponseType<CrmEmployeeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CrmEmployeeDto>> Get(string employeeId, CancellationToken ct) =>
        Ok(await crm.GetAsync(employeeId, ct) ?? throw new NotFoundException("No CRM employee has that ID."));
}
