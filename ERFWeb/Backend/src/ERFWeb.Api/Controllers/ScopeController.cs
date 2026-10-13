using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Scope assignments: who signs for whom (RBAC v0.6 flow 3). Level 1 scope is set by a Level 2
/// Signatory or the Business Owner; Level 2 scope by the Business Owner, with the bootstrap
/// exceptions decided in the service.
/// </summary>
[ApiController]
[Route("api/scope")]
[Authorize(Policy = Policies.EntitlementReader)]
[Produces("application/json")]
public sealed class ScopeController(IScopeService scope) : ControllerBase
{
    [HttpGet("assignments")]
    [ProducesResponseType<IReadOnlyList<ScopeAssignmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScopeAssignmentDto>>> List(
        [FromQuery] long? holderUserId = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] int? level = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await scope.ListAsync(new ScopeAssignmentQuery(holderUserId, departmentId, level, includeInactive), ct));

    [HttpGet("assignments/{id:long}")]
    [ProducesResponseType<ScopeAssignmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScopeAssignmentDto>> Get(long id, CancellationToken ct) =>
        Ok(await scope.GetAsync(id, ct));

    [HttpPost("assignments")]
    [Authorize(Policy = Policies.ScopeManager)]
    [ProducesResponseType<ScopeAssignmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ScopeAssignmentDto>> Create([FromBody] CreateScopeAssignmentRequest request, CancellationToken ct)
    {
        var created = await scope.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Changes members, limit, end date, or active flag. Holder, level, type, and department are fixed.</summary>
    [HttpPut("assignments/{id:long}")]
    [Authorize(Policy = Policies.ScopeManager)]
    [ProducesResponseType<ScopeAssignmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScopeAssignmentDto>> Update(long id, [FromBody] UpdateScopeAssignmentRequest request, CancellationToken ct) =>
        Ok(await scope.UpdateAsync(id, request, ct));

    /// <summary>Every active employee with their resolved Level 1 and Level 2 on the date (today by default).</summary>
    [HttpGet("coverage")]
    [ProducesResponseType<IReadOnlyList<ScopeCoverageRowDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScopeCoverageRowDto>>> Coverage([FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await scope.CoverageAsync(date, ct));
}
