using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// Role grants with dual approval (RBAC v0.6). A System Administrator grants; the grant waits
/// in Pending Activation until the designated Business Owner countersigns (spec 9.8). While no
/// Business Owner exists, a second System Administrator may countersign and the grant is flagged
/// as a bootstrap (plan open item 12.3a). Nobody changes roles on their own account (9.7), and
/// the three stacking rules are checked at save.
/// </summary>
public sealed class RoleGrantService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    UserDetailReader reader) : IRoleGrantService
{
    public async Task<UserDetailDto> GrantAsync(long userId, GrantRoleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);
        if (RoleRules.IsBaseRole(request.Role))
        {
            throw new ValidationException("role", "Every account already holds the Employee role; it cannot be granted.");
        }

        await tx.ExecuteAsync(async token =>
        {
            await EnsureUserAsync(userId, token);

            var existing = await db.UserRoleGrants.FirstOrDefaultAsync(g => g.UserId == userId && g.Role == request.Role, token);
            if (existing is not null && existing.Status != RoleGrantStatus.Revoked)
            {
                throw new ConflictException($"The account already holds {request.Role} ({existing.Status}).");
            }

            var held = await reader.HeldRolesAsync(userId, token);
            if (RoleRules.StackingViolation(held, request.Role) is { } violation)
            {
                throw new ValidationException("role", violation);
            }

            var now = clock.UtcNow;
            if (existing is null)
            {
                existing = new UserRoleGrant { UserId = userId, Role = request.Role };
                db.UserRoleGrants.Add(existing);
            }

            // A re-grant after a revoke starts the dual-approval trail again.
            existing.Status = RoleGrantStatus.PendingActivation;
            existing.GrantedByUserId = user.UserId;
            existing.GrantedAt = now;
            existing.CountersignedByUserId = null;
            existing.CountersignedAt = null;
            existing.RevokedByUserId = null;
            existing.RevokedAt = null;
            existing.IsBootstrap = false;
            existing.Note = Clean(request.Note);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.RoleGranted, null, user.UserId,
                JsonSerializer.Serialize(new { userId, role = request.Role.ToString(), note = existing.Note })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    public async Task<UserDetailDto> CountersignAsync(long userId, UserRole role, RoleGrantActionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await tx.ExecuteAsync(async token =>
        {
            var grant = await db.UserRoleGrants.FirstOrDefaultAsync(g => g.UserId == userId && g.Role == role, token)
                ?? throw new NotFoundException("Role grant not found.");
            if (grant.Status != RoleGrantStatus.PendingActivation)
            {
                throw new ConflictException($"The grant is {grant.Status}; only a Pending Activation grant can be countersigned.");
            }

            var businessOwnerExists = await reader.BusinessOwnerExistsAsync(token);
            var decision = RoleRules.Countersign(user.UserId, user.Roles, grant.GrantedByUserId ?? 0, userId, businessOwnerExists);
            if (!decision.IsAllowed)
            {
                throw new ForbiddenException(decision.Reason!);
            }

            grant.Status = RoleGrantStatus.Active;
            grant.CountersignedByUserId = user.UserId;
            grant.CountersignedAt = clock.UtcNow;
            grant.IsBootstrap = decision.IsBootstrap;
            if (Clean(request.Note) is { } note)
            {
                grant.Note = note;
            }

            db.SetRowVersion(grant, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.RoleCountersigned, null, user.UserId,
                JsonSerializer.Serialize(new { userId, role = role.ToString(), bootstrap = decision.IsBootstrap, grantedBy = grant.GrantedByUserId })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    public async Task<UserDetailDto> RevokeAsync(long userId, UserRole role, RoleGrantActionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RefuseSelfChange(userId);
        if (RoleRules.IsBaseRole(role))
        {
            throw new ValidationException("role", "The Employee role is never removed. Deactivate the account instead.");
        }

        await tx.ExecuteAsync(async token =>
        {
            var grant = await db.UserRoleGrants.FirstOrDefaultAsync(g => g.UserId == userId && g.Role == role, token)
                ?? throw new NotFoundException("Role grant not found.");
            if (grant.Status == RoleGrantStatus.Revoked)
            {
                throw new ConflictException("The grant is already revoked.");
            }

            grant.Status = RoleGrantStatus.Revoked;
            grant.RevokedByUserId = user.UserId;
            grant.RevokedAt = clock.UtcNow;
            if (Clean(request.Note) is { } note)
            {
                grant.Note = note;
            }

            db.SetRowVersion(grant, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(
                AdminAuditEvents.RoleRevoked, null, user.UserId,
                JsonSerializer.Serialize(new { userId, role = role.ToString(), note = grant.Note })), token);
        }, ct);

        return await reader.ReadAsync(userId, ct);
    }

    private void RefuseSelfChange(long targetUserId)
    {
        if (RoleRules.IsSelfChange(user.UserId, targetUserId))
        {
            throw new ForbiddenException("You cannot change the roles on your own account. Ask another System Administrator.");
        }
    }

    private async Task EnsureUserAsync(long userId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw new NotFoundException("User not found.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
