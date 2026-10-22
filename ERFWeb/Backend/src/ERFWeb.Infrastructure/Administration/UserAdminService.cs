using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// System Administrator account management. Every mutation runs in the serializable
/// transaction runner and writes an <c>Admin.*</c> audit row in the same transaction, so
/// Finance can review it later (spec 9.10). An administrator never changes their own
/// account (spec 9.7).
/// </summary>
public sealed class UserAdminService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    UserDetailReader reader) : IUserAdminService
{
    private const int MaxTextLength = 200;
    private const long MaxSignatureBytes = 2 * 1024 * 1024;
    private static readonly string[] SignatureContentTypes = ["image/png", "image/jpeg"];

    public async Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users =
            from u in db.Users.AsNoTracking()
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            select new { User = u, DepartmentName = d != null ? d.Name : null };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(x => EF.Functions.ILike(x.User.FullName, term) || EF.Functions.ILike(x.User.Username, term));
        }

        if (query.DepartmentId is { } departmentId)
        {
            users = users.Where(x => x.User.DepartmentId == departmentId);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(x => x.User.IsActive == isActive);
        }

        if (query.Role is { } role && role != UserRole.Employee)
        {
            users = users.Where(x => db.UserRoleGrants.Any(g => g.UserId == x.User.Id && g.Role == role && g.Status != RoleGrantStatus.Revoked));
        }

        var total = await users.CountAsync(ct);

        var ordered = (query.Sort?.ToLowerInvariant(), query.Direction) switch
        {
            ("username", SortDirection.Desc) => users.OrderByDescending(x => x.User.Username),
            ("username", _) => users.OrderBy(x => x.User.Username),
            ("department", SortDirection.Desc) => users.OrderByDescending(x => x.DepartmentName).ThenBy(x => x.User.FullName),
            ("department", _) => users.OrderBy(x => x.DepartmentName).ThenBy(x => x.User.FullName),
            ("created", SortDirection.Desc) => users.OrderByDescending(x => x.User.CreatedAt),
            ("created", _) => users.OrderBy(x => x.User.CreatedAt),
            (_, SortDirection.Desc) => users.OrderByDescending(x => x.User.FullName),
            _ => users.OrderBy(x => x.User.FullName),
        };

        var page = await ordered
            .Skip(query.Skip)
            .Take(query.NormalizedPageSize)
            .Select(x => new
            {
                x.User.Id,
                x.User.Username,
                x.User.FullName,
                x.User.Position,
                x.User.JobGrade,
                x.User.Email,
                x.User.MobileNo,
                x.User.DepartmentId,
                x.DepartmentName,
                x.User.IsActive,
                HasSignature = x.User.Signature != null && x.User.Signature.Length > 0,
                x.User.WorkWithEnabled,
                x.User.CreatedAt,
            })
            .ToListAsync(ct);

        var ids = page.Select(p => p.Id).ToList();
        var grants = await db.UserRoleGrants.AsNoTracking()
            .Where(g => ids.Contains(g.UserId) && g.Status != RoleGrantStatus.Revoked)
            .Select(g => new { g.UserId, g.Role, g.Status })
            .ToListAsync(ct);

        var items = page.Select(p => new UserListItemDto(
            p.Id, p.Username, p.FullName, p.Position, p.JobGrade, p.Email, p.MobileNo, p.DepartmentId, p.DepartmentName,
            p.IsActive, p.HasSignature, p.WorkWithEnabled, p.CreatedAt,
            ActiveRoles: new[] { UserRole.Employee }
                .Concat(grants.Where(g => g.UserId == p.Id && g.Status == RoleGrantStatus.Active).Select(g => g.Role).OrderBy(r => r))
                .ToList(),
            PendingRoles: grants.Where(g => g.UserId == p.Id && g.Status == RoleGrantStatus.PendingActivation).Select(g => g.Role).OrderBy(r => r).ToList()))
            .ToList();

        return new PagedResult<UserListItemDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

    public Task<UserDetailDto> GetAsync(long userId, CancellationToken ct = default) => reader.ReadAsync(userId, ct);

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new Dictionary<string, string[]>();
        if (AccountRules.UsernameError(request.Username) is { } usernameError)
        {
            errors["username"] = [usernameError];
        }

        if (AccountRules.PasswordError(request.Password) is { } passwordError)
        {
            errors["password"] = [passwordError];
        }

        ValidateProfile(request.FullName, request.Position, request.JobGrade, request.Email, request.MobileNo, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var username = request.Username.Trim();
        var created = await tx.ExecuteAsync(async token =>
        {
            if (await db.Users.AnyAsync(u => u.Username == username, token))
            {
                throw new ValidationException("username", "That username is already taken.");
            }

            await EnsureReferencesAsync(request.DepartmentId, request.CostCenterId, token);

            var entity = new User
            {
                Username = username,
                PasswordHash = Pbkdf2PasswordHasher.Hash(request.Password),
                FullName = request.FullName.Trim(),
                Position = Clean(request.Position),
                JobGrade = Clean(request.JobGrade),
                Email = Clean(request.Email),
                MobileNo = Clean(request.MobileNo),
                DepartmentId = request.DepartmentId,
                CostCenterId = request.CostCenterId,
                WorkWithEnabled = request.WorkWithEnabled,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = clock.UtcNow,
            };
            db.Users.Add(entity);
            await db.SaveChangesAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.UserCreated, null, user.UserId,
                JsonSerializer.Serialize(new { userId = entity.Id, entity.Username, entity.FullName, entity.DepartmentId })), token);
            return entity.Id;
        }, ct);

        return await reader.ReadAsync(created, ct);
    }

    public async Task<UserDetailDto> UpdateAsync(long userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);

        var errors = new Dictionary<string, string[]>();
        ValidateProfile(request.FullName, request.Position, request.JobGrade, request.Email, request.MobileNo, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        await tx.ExecuteAsync(async token =>
        {
            var entity = await LoadAsync(userId, token);
            await EnsureReferencesAsync(request.DepartmentId, request.CostCenterId, token);

            var before = Snapshot(entity);
            entity.FullName = request.FullName.Trim();
            entity.Position = Clean(request.Position);
            entity.JobGrade = Clean(request.JobGrade);
            entity.Email = Clean(request.Email);
            entity.MobileNo = Clean(request.MobileNo);
            entity.DepartmentId = request.DepartmentId;
            entity.CostCenterId = request.CostCenterId;
            entity.WorkWithEnabled = request.WorkWithEnabled;

            db.SetRowVersion(entity, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.UserUpdated, null, user.UserId,
                JsonSerializer.Serialize(new { userId, before, after = Snapshot(entity) })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    public async Task<UserDetailDto> SetActiveAsync(long userId, SetUserActiveRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);

        await tx.ExecuteAsync(async token =>
        {
            var entity = await LoadAsync(userId, token);
            if (entity.IsActive == request.IsActive)
            {
                throw new ConflictException(request.IsActive ? "The account is already active." : "The account is already deactivated.");
            }

            entity.IsActive = request.IsActive;
            db.SetRowVersion(entity, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                request.IsActive ? AdminAuditEvents.UserActivated : AdminAuditEvents.UserDeactivated, null, user.UserId,
                JsonSerializer.Serialize(new { userId, entity.Username })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    public async Task ResetPasswordAsync(long userId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);
        if (AccountRules.PasswordError(request.NewPassword) is { } passwordError)
        {
            throw new ValidationException("newPassword", passwordError);
        }

        await tx.ExecuteAsync(async token =>
        {
            var entity = await LoadAsync(userId, token);
            entity.PasswordHash = Pbkdf2PasswordHasher.Hash(request.NewPassword);
            entity.MustChangePassword = true;
            await db.SaveWithConcurrencyAsync(token);

            // A reset is also how an administrator unlocks a locked-out account.
            await db.LoginSecurities.Where(l => l.Username == entity.Username).ExecuteDeleteAsync(token);

            // The audit row records that a reset happened, never the credential itself.
            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.PasswordReset, null, user.UserId,
                JsonSerializer.Serialize(new { userId, entity.Username })), token);
        }, ct);
    }

    public async Task<UserDetailDto> SetSignatureAsync(long userId, SetSignatureRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);
        if (!SignatureContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("file", "The signature must be a PNG or JPEG image.");
        }

        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0 || buffer.Length > MaxSignatureBytes)
        {
            throw new ValidationException("file", $"The signature image must be between 1 byte and {MaxSignatureBytes / (1024 * 1024)} MB.");
        }

        var bytes = buffer.ToArray();
        await tx.ExecuteAsync(async token =>
        {
            var entity = await LoadAsync(userId, token);
            entity.Signature = bytes;
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.SignatureSet, null, user.UserId,
                JsonSerializer.Serialize(new { userId, fileName = Path.GetFileName(request.FileName), size = bytes.Length })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    public async Task<SignatureImageDto> GetSignatureAsync(long userId, CancellationToken ct = default)
    {
        var bytes = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Signature)
            .FirstOrDefaultAsync(ct);
        if (bytes is not { Length: > 0 })
        {
            throw new NotFoundException("No signature is on file.");
        }

        // Only PNG and JPEG are accepted on upload; the type is not stored, so read it from the header.
        var isPng = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
        return new SignatureImageDto(isPng ? "image/png" : "image/jpeg", bytes);
    }

    private void RefuseSelfChange(long targetUserId)
    {
        if (RoleRules.IsSelfChange(user.UserId, targetUserId))
        {
            throw new ForbiddenException("An administrator cannot change their own account. Ask another System Administrator.");
        }
    }

    private async Task<User> LoadAsync(long userId, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
        ?? throw new NotFoundException("User not found.");

    private async Task EnsureReferencesAsync(long? departmentId, long? costCenterId, CancellationToken ct)
    {
        if (departmentId is { } d && !await db.Departments.AnyAsync(x => x.Id == d && x.IsActive, ct))
        {
            throw new ValidationException("departmentId", "The department does not exist or is inactive.");
        }

        if (costCenterId is { } c && !await db.CostCenters.AnyAsync(x => x.Id == c && x.IsActive, ct))
        {
            throw new ValidationException("costCenterId", "The cost center does not exist or is inactive.");
        }
    }

    private static void ValidateProfile(string? fullName, string? position, string? jobGrade, string? email, string? mobileNo, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors["fullName"] = ["Full name is required."];
        }
        else if (fullName.Trim().Length > MaxTextLength)
        {
            errors["fullName"] = [$"Full name must be at most {MaxTextLength} characters."];
        }

        if (position is { Length: > MaxTextLength })
        {
            errors["position"] = [$"Position must be at most {MaxTextLength} characters."];
        }

        if (jobGrade is { Length: > 32 })
        {
            errors["jobGrade"] = ["Job grade must be at most 32 characters."];
        }

        if (!string.IsNullOrWhiteSpace(email) && (email.Length > MaxTextLength || !email.Contains('@')))
        {
            errors["email"] = ["Enter a valid email address."];
        }

        if (mobileNo is { Length: > 32 })
        {
            errors["mobileNo"] = ["Mobile number must be at most 32 characters."];
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object Snapshot(User u) => new
    {
        u.FullName,
        u.Position,
        u.JobGrade,
        u.Email,
        u.MobileNo,
        u.DepartmentId,
        u.CostCenterId,
        u.WorkWithEnabled,
    };
}
