using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Rate profiles (RBAC v0.6 flow 4): effective-dated, versioned entitlements assigned to a person,
/// job grade, department, or the company. Only a System Administrator changes them.
/// </summary>
[ApiController]
[Route("api/rates")]
[Authorize(Policy = Policies.EntitlementReader)]
[Produces("application/json")]
public sealed class RatesController(IRateService rates) : ControllerBase
{
    [HttpGet("profiles")]
    [ProducesResponseType<IReadOnlyList<RateProfileListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RateProfileListItemDto>>> Profiles([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await rates.ListProfilesAsync(includeInactive, ct));

    [HttpGet("profiles/{id:long}")]
    [ProducesResponseType<RateProfileDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RateProfileDetailDto>> Profile(long id, CancellationToken ct) =>
        Ok(await rates.GetProfileAsync(id, ct));

    [HttpPost("profiles")]
    [Authorize(Policy = Policies.RateManager)]
    [ProducesResponseType<RateProfileDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RateProfileDetailDto>> CreateProfile([FromBody] CreateRateProfileRequest request, CancellationToken ct)
    {
        var created = await rates.CreateProfileAsync(request, ct);
        return CreatedAtAction(nameof(Profile), new { id = created.Id }, created);
    }

    [HttpPut("profiles/{id:long}")]
    [Authorize(Policy = Policies.RateManager)]
    [ProducesResponseType<RateProfileDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RateProfileDetailDto>> UpdateProfile(long id, [FromBody] UpdateRateProfileRequest request, CancellationToken ct) =>
        Ok(await rates.UpdateProfileAsync(id, request, ct));

    /// <summary>Adds a new version of one category rate; the open version is closed the day before.</summary>
    [HttpPost("profiles/{id:long}/lines")]
    [Authorize(Policy = Policies.RateManager)]
    [ProducesResponseType<RateProfileDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RateProfileDetailDto>> AddLine(long id, [FromBody] AddRateLineRequest request, CancellationToken ct) =>
        Ok(await rates.AddLineAsync(id, request, ct));

    [HttpGet("assignments")]
    [ProducesResponseType<IReadOnlyList<RateAssignmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RateAssignmentDto>>> Assignments(
        [FromQuery] long? profileId = null,
        [FromQuery] RateTargetType? targetType = null,
        [FromQuery] bool includeEnded = false,
        CancellationToken ct = default) =>
        Ok(await rates.ListAssignmentsAsync(new RateAssignmentQuery(profileId, targetType, includeEnded), ct));

    [HttpPost("assignments")]
    [Authorize(Policy = Policies.RateManager)]
    [ProducesResponseType<RateAssignmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RateAssignmentDto>> CreateAssignment([FromBody] CreateRateAssignmentRequest request, CancellationToken ct)
    {
        var created = await rates.CreateAssignmentAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("assignments/{id:long}/end")]
    [Authorize(Policy = Policies.RateManager)]
    [ProducesResponseType<RateAssignmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RateAssignmentDto>> EndAssignment(long id, [FromBody] EndRateAssignmentRequest request, CancellationToken ct) =>
        Ok(await rates.EndAssignmentAsync(id, request, ct));

    /// <summary>The profile and lines in force for a person on a date (today by default).</summary>
    [HttpGet("resolve")]
    [ProducesResponseType<ResolvedRatesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResolvedRatesDto>> Resolve([FromQuery] long userId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await rates.ResolveAsync(userId, date, ct));
}
