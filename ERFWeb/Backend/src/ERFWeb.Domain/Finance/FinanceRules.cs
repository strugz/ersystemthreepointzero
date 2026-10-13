namespace ERFWeb.Domain.Finance;

/// <summary>
/// Physical-receipt tracking after approval (plan section 13, Phase 5). Finance records whether the
/// receipts arrived; "received" is a one-time confirmation that is never reverted, and only a
/// report with all receipts received can be cleared for payment, which is final. Finance never
/// acts on its own report, and never rejects on merit (9.4): that belongs to the approvers.
/// </summary>
public static class FinanceRules
{
    public const int MaxRemarksLength = 1000;

    /// <summary>Statuses Finance may record for the receipts themselves.</summary>
    public static readonly IReadOnlySet<FinanceStatus> ReceiptStatuses = new HashSet<FinanceStatus>
    {
        FinanceStatus.Pending,
        FinanceStatus.ReceiptsIncomplete,
        FinanceStatus.ReceiptsReceived,
    };

    /// <summary>Null when Finance may move a report's receipts from <paramref name="current"/> to <paramref name="target"/>.</summary>
    public static string? ReceiptTransitionError(FinanceStatus current, FinanceStatus target, string? remarks)
    {
        if (!ReceiptStatuses.Contains(target))
        {
            return "Use Clear for payment to release the report.";
        }

        if (current is FinanceStatus.ReceiptsReceived or FinanceStatus.ClearedForPayment)
        {
            return "Receipts were already confirmed as received; that cannot be changed.";
        }

        if (current == target)
        {
            return $"The receipts are already marked {target}.";
        }

        if (target == FinanceStatus.ReceiptsIncomplete && string.IsNullOrWhiteSpace(remarks))
        {
            return "Say which receipts are missing.";
        }

        return RemarksError(remarks);
    }

    /// <summary>Null when the report may be cleared for payment.</summary>
    public static string? ClearError(FinanceStatus current, string? remarks) =>
        current switch
        {
            FinanceStatus.ClearedForPayment => "The report is already cleared for payment.",
            FinanceStatus.ReceiptsReceived => RemarksError(remarks),
            _ => "Confirm that all receipts were received before clearing the report for payment.",
        };

    /// <summary>Follow-ups go to employees whose receipts are still outstanding.</summary>
    public static bool AwaitsReceipts(FinanceStatus status) =>
        status is FinanceStatus.Pending or FinanceStatus.ReceiptsIncomplete;

    public static string? RemarksError(string? remarks) =>
        remarks is { Length: > MaxRemarksLength } ? $"Remarks must be at most {MaxRemarksLength} characters." : null;
}
