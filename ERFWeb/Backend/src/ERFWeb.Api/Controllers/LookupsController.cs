using ERFWeb.Application.Features.Lookups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Reference data for the report editor, and the signed-in employee's saved vehicle.</summary>
[ApiController]
[Route("api/lookups")]
[Authorize]
[Produces("application/json")]
public sealed class LookupsController(ILookupService lookups) : ControllerBase
{
    /// <summary>Colleagues selectable in the "Work With" picker (active users flagged as work-with enabled).</summary>
    [HttpGet("work-with")]
    [ProducesResponseType<IReadOnlyList<WorkWithOptionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkWithOptionDto>>> WorkWith([FromQuery] string? search, CancellationToken ct) =>
        Ok(await lookups.WorkWithAsync(search, ct));

    /// <summary>Active departments for pickers (any signed-in user).</summary>
    [HttpGet("departments")]
    [ProducesResponseType<IReadOnlyList<DepartmentOptionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentOptionDto>>> Departments(CancellationToken ct) =>
        Ok(await lookups.DepartmentsAsync(ct));

    /// <summary>Active fare modes for the fare-leg picker (any signed-in user).</summary>
    [HttpGet("fare-modes")]
    [ProducesResponseType<IReadOnlyList<FareModeOptionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FareModeOptionDto>>> FareModes(CancellationToken ct) =>
        Ok(await lookups.FareModesAsync(ct));

    /// <summary>The signed-in employee's meal/transport rates (zeros when none are configured), for the report's department when given.</summary>
    [HttpGet("my-rates")]
    [ProducesResponseType<EmployeeRatesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeRatesDto>> MyRates([FromQuery] DateOnly? date, [FromQuery] long? departmentId, CancellationToken ct) =>
        Ok(await lookups.MyRatesAsync(date, departmentId, ct));

    /// <summary>Whether the signed-in employee can file: signature on record and approvers configured (for the report's department when given).</summary>
    [HttpGet("my-filing-readiness")]
    [ProducesResponseType<MyFilingReadinessDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyFilingReadinessDto>> MyFilingReadiness([FromQuery] long? departmentId, CancellationToken ct) =>
        Ok(await lookups.MyFilingReadinessAsync(departmentId, ct));

    /// <summary>The signed-in employee's saved vehicle (model and plate number), filled into new Gas and Vehicle emergency purchase lines.</summary>
    [HttpGet("my-vehicle")]
    [ProducesResponseType<MyVehicleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyVehicleDto>> MyVehicle(CancellationToken ct) =>
        Ok(await lookups.MyVehicleAsync(ct));

    /// <summary>Saves the signed-in employee's own vehicle (both empty clears it); 400 on <c>model</c> or <c>plateNo</c>.</summary>
    [HttpPut("my-vehicle")]
    [ProducesResponseType<MyVehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyVehicleDto>> UpdateMyVehicle([FromBody] UpdateMyVehicleRequest request, CancellationToken ct) =>
        Ok(await lookups.UpdateMyVehicleAsync(request, ct));
}
