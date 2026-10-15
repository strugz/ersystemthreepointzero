using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Delegations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Approval delegation for signatories. The service checks the level rule, overlap, and that a
/// Level 2 assigning for someone else is the Level 2 over that Level 1.
/// </summary>
[ApiController]
[Route("api/delegations")]
[Authorize(Policy = Policies.Approver)]
[Produces("application/json")]
public sealed class DelegationsController(IDelegationService delegations) : ControllerBase
{
    /// <summary>Delegations I gave, received, or assigned for a Level 1.</summary>
    [HttpGet]
    [ProducesResponseType<MyDelegationsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyDelegationsDto>> Mine(CancellationToken ct) =>
        Ok(await delegations.MineAsync(ct));

    /// <summary>Who can stand in, and the Level 1s I may assign a delegate for.</summary>
    [HttpGet("options")]
    [ProducesResponseType<DelegationOptionsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DelegationOptionsDto>> Options(CancellationToken ct) =>
        Ok(await delegations.OptionsAsync(ct));

    [HttpPost]
    [ProducesResponseType<DelegationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelegationDto>> Create([FromBody] CreateDelegationRequest request, CancellationToken ct) =>
        Ok(await delegations.CreateAsync(request, ct));

    [HttpPost("{delegationId:long}/cancel")]
    [ProducesResponseType<DelegationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DelegationDto>> Cancel(long delegationId, [FromBody] CancelDelegationRequest request, CancellationToken ct) =>
        Ok(await delegations.CancelAsync(delegationId, request, ct));
}
