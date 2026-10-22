using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>System Administrator console: accounts and role grants (RBAC v0.6, flows 1 and 2).</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class AdminUsersController(IUserAdminService users, IRoleGrantService roles) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<UserListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? sort = null,
        [FromQuery] SortDirection direction = SortDirection.Asc,
        [FromQuery] string? search = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = new UserListQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Direction = direction,
            Search = search,
            DepartmentId = departmentId,
            Role = role,
            IsActive = isActive,
        };
        return Ok(await users.ListAsync(query, ct));
    }

    [HttpGet("{userId:long}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> Get(long userId, CancellationToken ct) =>
        Ok(await users.GetAsync(userId, ct));

    /// <summary>Registers an account. It holds Employee immediately and may file after the cooling period.</summary>
    [HttpPost]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDetailDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var created = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { userId = created.Id }, created);
    }

    [HttpPut("{userId:long}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Update(long userId, [FromBody] UpdateUserRequest request, CancellationToken ct) =>
        Ok(await users.UpdateAsync(userId, request, ct));

    /// <summary>Deactivates or reactivates an account. Deactivation keeps the row and its history.</summary>
    [HttpPost("{userId:long}/active")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> SetActive(long userId, [FromBody] SetUserActiveRequest request, CancellationToken ct) =>
        Ok(await users.SetActiveAsync(userId, request, ct));

    [HttpPost("{userId:long}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(long userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await users.ResetPasswordAsync(userId, request, ct);
        return NoContent();
    }

    /// <summary>The signature image on file, for the preview on the account page.</summary>
    [HttpGet("{userId:long}/signature")]
    [Produces("image/png", "image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSignature(long userId, CancellationToken ct)
    {
        var image = await users.GetSignatureAsync(userId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(image.Content, image.ContentType);
    }

    /// <summary>Loads the employee's signature image (PNG or JPEG) printed on the R47. Field name: <c>file</c>.</summary>
    [HttpPost("{userId:long}/signature")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetailDto>> SetSignature(long userId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException("file", "Choose a signature image to upload.");
        }

        await using var stream = file.OpenReadStream();
        return Ok(await users.SetSignatureAsync(userId, new SetSignatureRequest(file.FileName, file.ContentType, stream), ct));
    }

    // ---------- Role grants ----------

    /// <summary>Grants a role. Privileged roles wait as Pending Activation until countersigned.</summary>
    [HttpPost("{userId:long}/roles")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDetailDto>> Grant(long userId, [FromBody] GrantRoleRequest request, CancellationToken ct) =>
        Ok(await roles.GrantAsync(userId, request, ct));

    /// <summary>Countersigns a Pending Activation grant (Business Owner, or a second administrator as bootstrap).</summary>
    [HttpPost("{userId:long}/roles/{role}/countersign")]
    [Authorize(Policy = Policies.Countersigner)]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Countersign(long userId, UserRole role, [FromBody] RoleGrantActionRequest request, CancellationToken ct) =>
        Ok(await roles.CountersignAsync(userId, role, request, ct));

    [HttpPost("{userId:long}/roles/{role}/revoke")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Revoke(long userId, UserRole role, [FromBody] RoleGrantActionRequest request, CancellationToken ct) =>
        Ok(await roles.RevokeAsync(userId, role, request, ct));
}
