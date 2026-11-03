using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// Finance approves or rejects each signature image before the R47 prints it. Each decision runs in the
/// transaction runner and writes an <c>Admin.Signature*</c> audit row (ids, names and the note only, never
/// the image), so it shows in the administration audit log.
/// </summary>
public sealed class SignatureApprovalService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit) : ISignatureApprovalService
{
    private const int MaxNoteLength = 500;

    public async Task<IReadOnlyList<SignatureReviewItemDto>> ListAsync(SignatureStatus status, CancellationToken ct = default)
    {
        var withImage = db.Users.AsNoTracking().Where(u => u.Signature != null && u.Signature.Length > 0);
        withImage = status switch
        {
            SignatureStatus.Pending => withImage.Where(u => u.SignatureApprovedAt == null),
            SignatureStatus.Approved => withImage.Where(u => u.SignatureApprovedAt != null),
            _ => throw new ValidationException("status", "Choose Pending or Approved."),
        };

        return await ItemsAsync(withImage, ct);
    }

    public async Task<SignatureImageDto> GetImageAsync(long userId, CancellationToken ct = default)
    {
        var bytes = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Signature).FirstOrDefaultAsync(ct);
        return bytes is { Length: > 0 } ? SignatureImages.ToDto(bytes) : throw new NotFoundException("No signature is on file.");
    }

    public Task<SignatureReviewItemDto> ApproveAsync(long userId, SignatureReviewRequest request, CancellationToken ct = default) =>
        ReviewAsync(userId, request, approve: true, ct);

    public Task<SignatureReviewItemDto> RejectAsync(long userId, SignatureReviewRequest request, CancellationToken ct = default) =>
        ReviewAsync(userId, request, approve: false, ct);

    private async Task<SignatureReviewItemDto> ReviewAsync(long userId, SignatureReviewRequest request, bool approve, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note is { Length: > MaxNoteLength })
        {
            throw new ValidationException("note", $"Use at most {MaxNoteLength} characters.");
        }

        if (SignatureApprovalRules.IsSelfReview(user.UserId, userId))
        {
            throw new ForbiddenException(SignatureApprovalRules.SelfReviewMessage);
        }

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, token)
                ?? throw new NotFoundException("User not found.");

            var blocker = SignatureApprovalRules.ReviewBlocker(
                entity.Signature is { Length: > 0 }, entity.SignatureUploadedAt, entity.SignatureApprovedAt, request.UploadedAt);
            if (blocker is not null)
            {
                throw new ConflictException(blocker);
            }

            if (approve)
            {
                entity.SignatureApprovedAt = clock.UtcNow;
                entity.SignatureApprovedByUserId = user.UserId;
            }
            else
            {
                // A rejected image is removed; the person loads a new one through the administrator.
                entity.Signature = null;
                entity.SignatureUploadedAt = null;
            }

            await db.SaveWithConcurrencyAsync(token);
            await audit.WriteAsync(new WorkflowAuditEntry(
                approve ? AdminAuditEvents.SignatureApproved : AdminAuditEvents.SignatureRejected, null, user.UserId,
                JsonSerializer.Serialize(new { userId, entity.Username, note })), token);
        }, ct);

        return (await ItemsAsync(db.Users.AsNoTracking().Where(u => u.Id == userId), ct))[0];
    }

    private async Task<IReadOnlyList<SignatureReviewItemDto>> ItemsAsync(IQueryable<User> users, CancellationToken ct)
    {
        var rows = await (
            from u in users
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            join aj in db.Users.AsNoTracking() on u.SignatureApprovedByUserId equals (long?)aj.Id into az
            from a in az.DefaultIfEmpty()
            orderby u.FullName
            select new
            {
                u.Id,
                u.FullName,
                DepartmentName = d != null ? d.Name : null,
                HasImage = u.Signature != null && u.Signature!.Length > 0,
                u.SignatureUploadedAt,
                u.SignatureApprovedAt,
                ApprovedByName = a != null ? a.FullName : null,
            }).ToListAsync(ct);

        return rows.Select(r => new SignatureReviewItemDto(
            r.Id, r.FullName, r.DepartmentName, SignatureApprovalRules.Status(r.HasImage, r.SignatureApprovedAt),
            r.SignatureUploadedAt, r.SignatureApprovedAt, r.ApprovedByName)).ToList();
    }
}
