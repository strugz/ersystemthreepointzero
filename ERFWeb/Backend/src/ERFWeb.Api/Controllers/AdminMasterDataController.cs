using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>System Administrator console: master data (flow 1, step 2). Rows are deactivated, never deleted.</summary>
[ApiController]
[Route("api/admin/master-data")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminMasterDataController(IMasterDataService masterData, IFareModeAdminService fareModes) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> Departments([FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await masterData.ListDepartmentsAsync(includeInactive, ct));

    [HttpPost("departments")]
    [ProducesResponseType<DepartmentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] SaveDepartmentRequest request, CancellationToken ct)
    {
        var created = await masterData.CreateDepartmentAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("departments/{id:long}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(long id, [FromBody] SaveDepartmentRequest request, CancellationToken ct) =>
        Ok(await masterData.UpdateDepartmentAsync(id, request, ct));

    [HttpGet("cost-centers")]
    public async Task<ActionResult<IReadOnlyList<CostCenterDto>>> CostCenters([FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await masterData.ListCostCentersAsync(includeInactive, ct));

    [HttpPost("cost-centers")]
    [ProducesResponseType<CostCenterDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CostCenterDto>> CreateCostCenter([FromBody] SaveCostCenterRequest request, CancellationToken ct)
    {
        var created = await masterData.CreateCostCenterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("cost-centers/{id:long}")]
    public async Task<ActionResult<CostCenterDto>> UpdateCostCenter(long id, [FromBody] SaveCostCenterRequest request, CancellationToken ct) =>
        Ok(await masterData.UpdateCostCenterAsync(id, request, ct));

    [HttpGet("currencies")]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> Currencies([FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await masterData.ListCurrenciesAsync(includeInactive, ct));

    [HttpPost("currencies")]
    [ProducesResponseType<CurrencyDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CurrencyDto>> CreateCurrency([FromBody] SaveCurrencyRequest request, CancellationToken ct)
    {
        var created = await masterData.CreateCurrencyAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("currencies/{code}")]
    public async Task<ActionResult<CurrencyDto>> UpdateCurrency(string code, [FromBody] SaveCurrencyRequest request, CancellationToken ct) =>
        Ok(await masterData.UpdateCurrencyAsync(code, request, ct));

    [HttpGet("sites")]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> Sites([FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await masterData.ListSitesAsync(includeInactive, ct));

    [HttpPost("sites")]
    [ProducesResponseType<SiteDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SiteDto>> CreateSite([FromBody] SaveSiteRequest request, CancellationToken ct)
    {
        var created = await masterData.CreateSiteAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("sites/{id:long}")]
    public async Task<ActionResult<SiteDto>> UpdateSite(long id, [FromBody] SaveSiteRequest request, CancellationToken ct) =>
        Ok(await masterData.UpdateSiteAsync(id, request, ct));

    [HttpGet("fare-modes")]
    public async Task<ActionResult<IReadOnlyList<FareModeDto>>> FareModes([FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await fareModes.ListAsync(includeInactive, ct));

    [HttpPost("fare-modes")]
    [ProducesResponseType<FareModeDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<FareModeDto>> CreateFareMode([FromBody] SaveFareModeRequest request, CancellationToken ct)
    {
        var created = await fareModes.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("fare-modes/{id:long}")]
    public async Task<ActionResult<FareModeDto>> UpdateFareMode(long id, [FromBody] SaveFareModeRequest request, CancellationToken ct) =>
        Ok(await fareModes.UpdateAsync(id, request, ct));

    /// <summary>The expense categories are fixed by the R47 form; listed here for the console.</summary>
    [HttpGet("expense-categories")]
    public ActionResult<IReadOnlyList<ExpenseCategoryDto>> ExpenseCategories() =>
        Ok(masterData.ListExpenseCategories());
}
