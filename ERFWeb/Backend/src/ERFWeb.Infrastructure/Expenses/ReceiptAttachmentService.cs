using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Scanned-receipt attachments stored as <c>bytea</c>. Upload/delete follow the report edit
/// rules; download follows the report view policy (approvers and Finance can see receipts).
/// </summary>
public sealed class ReceiptAttachmentService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    ReportEditGate gate,
    IReportAccessPolicy access,
    ExpenseReportDetailLoader loader,
    IOptions<AttachmentOptions> options) : IReceiptAttachmentService
{
    private readonly AttachmentOptions _options = options.Value;

    public async Task<IReadOnlyList<AttachmentDto>> ListAsync(Guid reportId, CancellationToken ct = default)
    {
        await EnsureCanViewAsync(reportId, ct);
        return await loader.LoadAttachmentsAsync(reportId, ct);
    }

    public async Task<AttachmentDto> UploadAsync(Guid reportId, UploadAttachmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var id = await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);

            var count = await db.ReceiptAttachments.CountAsync(a => a.ReportId == reportId, token);
            if (count >= _options.MaxFilesPerReport)
            {
                throw new ValidationException("file", $"A report can have at most {_options.MaxFilesPerReport} attachments.");
            }

            var attachment = new ReceiptAttachment
            {
                ReportId = reportId,
                FileName = Path.GetFileName(request.FileName.Trim()),
                ContentType = request.ContentType.Trim().ToLowerInvariant(),
                SizeBytes = request.Content.LongLength,
                Content = request.Content,
                UploadedByUserId = user.UserId,
                UploadedAt = clock.UtcNow,
            };
            db.ReceiptAttachments.Add(attachment);
            report.UpdatedAt = attachment.UploadedAt;
            await db.SaveChangesAsync(token);
            return attachment.Id;
        }, ct);

        var list = await loader.LoadAttachmentsAsync(reportId, ct);
        return list.First(a => a.Id == id);
    }

    public async Task<AttachmentContentDto> DownloadAsync(Guid reportId, long attachmentId, CancellationToken ct = default)
    {
        await EnsureCanViewAsync(reportId, ct);

        var row = await db.ReceiptAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId && a.ReportId == reportId)
            .Select(a => new AttachmentContentDto(a.FileName, a.ContentType, a.Content))
            .FirstOrDefaultAsync(ct);

        return row ?? throw new NotFoundException("Attachment not found.");
    }

    public Task DeleteAsync(Guid reportId, long attachmentId, CancellationToken ct = default) =>
        tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            var attachment = await db.ReceiptAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.ReportId == reportId, token)
                ?? throw new NotFoundException("Attachment not found.");

            db.ReceiptAttachments.Remove(attachment);
            report.UpdatedAt = clock.UtcNow;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

    private void Validate(UploadAttachmentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            errors["fileName"] = ["A file name is required."];
        }

        if (request.Content is null || request.Content.Length == 0)
        {
            errors["file"] = ["The uploaded file is empty."];
        }
        else if (request.Content.LongLength > _options.MaxFileSizeBytes)
        {
            errors["file"] = [$"The file exceeds the {_options.MaxFileSizeMb} MB limit."];
        }

        if (!_options.AllowedContentTypes.Contains(request.ContentType?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            errors["contentType"] = [$"Only these file types are allowed: {string.Join(", ", _options.AllowedContentTypes)}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task EnsureCanViewAsync(Guid reportId, CancellationToken ct)
    {
        if (!await access.CanViewAsync(reportId, ct))
        {
            throw new ForbiddenException("You are not allowed to view this report's attachments.");
        }
    }
}
