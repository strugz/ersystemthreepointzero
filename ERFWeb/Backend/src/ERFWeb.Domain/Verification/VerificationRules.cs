using ERFWeb.Domain.Approval;

namespace ERFWeb.Domain.Verification;

/// <summary>A refusal (or <see cref="Ok"/>) from a verification rule, mapped to 403 / 409 / 400 by the service.</summary>
public sealed record VerificationCheck(WorkflowRefusal Refusal, string? Reason)
{
    public static readonly VerificationCheck Ok = new(WorkflowRefusal.None, null);

    public bool IsAllowed => Refusal == WorkflowRefusal.None;

    public static VerificationCheck Forbidden(string reason) => new(WorkflowRefusal.Forbidden, reason);

    public static VerificationCheck Conflict(string reason) => new(WorkflowRefusal.Conflict, reason);

    public static VerificationCheck Invalid(string reason) => new(WorkflowRefusal.Invalid, reason);
}

/// <summary>One verification request as the rules see it.</summary>
public sealed record VerificationState(
    VerificationStatus Status,
    long ClaimantUserId,
    long RequestedByUserId,
    long? VerifierUserId,
    VerificationFinding? Finding,
    long? QueryToUserId);

/// <summary>
/// Verification and queries (plan Phase 6, Flow 6). Finance flags a doubtful line on an approved
/// report (9.18 post-approval only) and the report cannot be cleared while the request is unresolved.
/// An Expense Verifier picks it up and records Verified, Verified with Note, or Not Verified, once
/// (9.17): they never change amounts, approve, or reject. Nobody verifies their own report or one
/// they approved (9.16). A Not Verified finding goes back to Finance, who queries the Level 2
/// Signatory; Level 2 decides to resume, return, or reject (9.4: Finance never rejects on merit).
/// </summary>
public static class VerificationRules
{
    public const int MaxTextLength = 2000;

    /// <summary>A request still holds up clearance until it is closed.</summary>
    public static bool IsUnresolved(VerificationStatus status) => status != VerificationStatus.Closed;

    /// <summary>Finance flags a line on an approved report that is not yet cleared for payment.</summary>
    public static VerificationCheck CanRaise(
        ReportStatus reportStatus,
        FinanceStatus? financeStatus,
        long claimantUserId,
        long actorUserId,
        bool lineAlreadyUnderVerification,
        string? reason)
    {
        if (actorUserId == claimantUserId)
        {
            return VerificationCheck.Forbidden("You cannot raise a verification on your own report. Another Finance user must do it.");
        }

        if (reportStatus != ReportStatus.Approved || financeStatus is null)
        {
            return VerificationCheck.Conflict("Verification is only for approved reports in the Finance queue.");
        }

        if (financeStatus == FinanceStatus.ClearedForPayment)
        {
            return VerificationCheck.Conflict("The report is already cleared for payment.");
        }

        if (lineAlreadyUnderVerification)
        {
            return VerificationCheck.Conflict("This line already has an open verification request.");
        }

        return TextCheck(reason, required: true, "Say what Finance doubts about this line.");
    }

    /// <summary>
    /// A verifier takes an open request. They may not verify their own report or one they approved
    /// (9.16); Finance never holds the verifier role (stacking rule), so the requester is excluded too.
    /// </summary>
    public static VerificationCheck CanPickUp(VerificationState request, IReadOnlySet<long> approverUserIds, long actorUserId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(approverUserIds);
        if (IsConflicted(request, approverUserIds, actorUserId))
        {
            return VerificationCheck.Forbidden("You cannot verify your own report or one you approved.");
        }

        return request.Status == VerificationStatus.Open
            ? VerificationCheck.Ok
            : VerificationCheck.Conflict("This request was already picked up.");
    }

    /// <summary>Only the verifier who picked the request up records its finding, once.</summary>
    public static VerificationCheck CanRecordFinding(VerificationState request, long actorUserId, VerificationFinding finding, string? note)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.VerifierUserId != actorUserId)
        {
            return VerificationCheck.Forbidden("Only the verifier who picked up this request can record the finding.");
        }

        if (request.Status != VerificationStatus.InProgress)
        {
            return VerificationCheck.Conflict("A finding was already recorded; findings cannot be changed.");
        }

        return TextCheck(note, required: NeedsNote(finding), "Write what you found.");
    }

    /// <summary>Verified and Verified with Note close the request; Not Verified goes back to Finance to query Level 2.</summary>
    public static VerificationStatus StatusAfterFinding(VerificationFinding finding) =>
        finding == VerificationFinding.NotVerified ? VerificationStatus.Found : VerificationStatus.Closed;

    public static bool NeedsNote(VerificationFinding finding) => finding != VerificationFinding.Verified;

    /// <summary>Finance refers a Not Verified finding to Level 2 for a merit decision.</summary>
    public static VerificationCheck CanQuery(VerificationState request, long actorUserId, string? note)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorUserId == request.ClaimantUserId)
        {
            return VerificationCheck.Forbidden("You cannot query your own report.");
        }

        if (request.Status != VerificationStatus.Found || request.Finding != VerificationFinding.NotVerified)
        {
            return VerificationCheck.Conflict("Only a Not Verified finding waiting for Finance can be queried.");
        }

        return TextCheck(note, required: true, "Say what Level 2 should decide.");
    }

    /// <summary>The queried Level 2 decides: resume, return (reason), or reject (reason).</summary>
    public static VerificationCheck CanDecide(VerificationState request, long actorUserId, QueryDisposition disposition, string? note)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorUserId == request.ClaimantUserId || request.QueryToUserId != actorUserId)
        {
            return VerificationCheck.Forbidden("This query is not addressed to you.");
        }

        if (request.Status != VerificationStatus.Queried)
        {
            return VerificationCheck.Conflict("This query was already decided.");
        }

        return TextCheck(note, required: disposition != QueryDisposition.Resume, "Give the reason for your decision.");
    }

    /// <summary>The verifier is the claimant, the requester, or approved the report (9.16).</summary>
    public static bool IsConflicted(VerificationState request, IReadOnlySet<long> approverUserIds, long userId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(approverUserIds);
        return userId == request.ClaimantUserId || userId == request.RequestedByUserId || approverUserIds.Contains(userId);
    }

    private static VerificationCheck TextCheck(string? text, bool required, string missing)
    {
        if (required && string.IsNullOrWhiteSpace(text))
        {
            return VerificationCheck.Invalid(missing);
        }

        return text is { Length: > MaxTextLength }
            ? VerificationCheck.Invalid($"Keep it to {MaxTextLength} characters.")
            : VerificationCheck.Ok;
    }
}
