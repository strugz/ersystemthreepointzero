using ERFWeb.Application.Features.Lookups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Reference data for the report editor.</summary>
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

    /// <summary>The signed-in employee's meal/transport rates (zeros when none are configured).</summary>
    [HttpGet("my-rates")]
    [ProducesResponseType<EmployeeRatesDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeRatesDto>> MyRates([FromQuery] DateOnly? date, CancellationToken ct) =>
        Ok(await lookups.MyRatesAsync(date, ct));

    /// <summary>Whether the signed-in employee can file: signature on record and approvers configured.</summary>
    [HttpGet("my-filing-readiness")]
    [ProducesResponseType<MyFilingReadinessDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MyFilingReadinessDto>> MyFilingReadiness(CancellationToken ct) =>
        Ok(await lookups.MyFilingReadinessAsync(ct));
}
