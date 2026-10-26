namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Pure rules for a single expense line. Mirrors the established ERF behaviour:
/// the line total is the entered amount times the multiplier (per-diem days), and
/// VAT is a separately entered memo figure that is only allowed when an Invoice/OR
/// number is present.
/// </summary>
public static class ExpenseLineRules
{
    public const int MaxParticularsLength = 200;
    public const int MaxShortTextLength = 100;

    /// <summary>
    /// Line total = amount × multiplier, rounded the way the desktop ERF rounds it:
    /// a line backed by an Invoice/OR number with multiplier 1 keeps centavos (the receipt
    /// amount is exact); every other line — per-diem, allowances, multiplied amounts,
    /// receipt-less entries — is rounded to whole pesos. The desktop used VB's
    /// <c>Math.Round</c>, which rounds midpoints to even, so we do the same.
    /// </summary>
    public static decimal Total(decimal amount, int multiplier, bool hasInvoice)
    {
        if (multiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be at least 1.");
        }

        var raw = amount * multiplier;
        return hasInvoice && multiplier == 1
            ? Math.Round(raw, 2, MidpointRounding.ToEven)
            : Math.Round(raw, 0, MidpointRounding.ToEven);
    }

    /// <summary>VAT may only be recorded when the line has an Invoice/OR number.</summary>
    public static bool AllowsVat(string? invoiceOrNo) => !string.IsNullOrWhiteSpace(invoiceOrNo);

    /// <summary>
    /// A new line order (the order the R47 prints) must name every active line of the report exactly
    /// once; anything else means the client's list is stale (a line was added or removed meanwhile).
    /// </summary>
    public static bool IsCompleteOrder(IReadOnlyCollection<long> orderedLineIds, IReadOnlyCollection<long> activeLineIds)
    {
        ArgumentNullException.ThrowIfNull(orderedLineIds);
        ArgumentNullException.ThrowIfNull(activeLineIds);
        return orderedLineIds.Count == activeLineIds.Count
            && orderedLineIds.Distinct().Count() == orderedLineIds.Count
            && orderedLineIds.All(activeLineIds.Contains);
    }

    /// <summary>
    /// Validates the business fields of a line. Returns field → message pairs; empty when valid.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> Validate(
        DateOnly transactionDate,
        string? particulars,
        decimal amount,
        decimal vatAmount,
        string? invoiceOrNo,
        int multiplier,
        bool isPerdiem,
        int? totalDays,
        int? minusDays,
        DateOnly? reportDateFrom,
        DateOnly? reportDateTo)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(particulars))
        {
            Add(errors, "particulars", "Particulars are required.");
        }
        else if (particulars.Length > MaxParticularsLength)
        {
            Add(errors, "particulars", $"Particulars must be {MaxParticularsLength} characters or fewer.");
        }

        if (amount <= 0)
        {
            Add(errors, "amount", "Amount must be greater than zero.");
        }

        if (vatAmount < 0)
        {
            Add(errors, "vatAmount", "VAT amount cannot be negative.");
        }
        else if (vatAmount > 0 && !AllowsVat(invoiceOrNo))
        {
            Add(errors, "vatAmount", "VAT can only be entered when an Invoice/OR number is provided.");
        }

        if (multiplier < 1)
        {
            Add(errors, "multiplier", "Multiplier must be at least 1.");
        }

        if (isPerdiem)
        {
            if (totalDays is null or < 1)
            {
                Add(errors, "totalDays", "Total days is required for a per-diem line.");
            }

            if (minusDays is < 0)
            {
                Add(errors, "minusDays", "Minus days cannot be negative.");
            }

            if (totalDays is not null && minusDays is not null && totalDays - minusDays < 1)
            {
                Add(errors, "minusDays", "0 or less than 0 multiplier is not allowed.");
            }
        }

        if (reportDateFrom is not null && transactionDate < reportDateFrom)
        {
            Add(errors, "transactionDate", "Transaction date is before the report period.");
        }

        if (reportDateTo is not null && transactionDate > reportDateTo)
        {
            Add(errors, "transactionDate", "Transaction date is after the report period.");
        }

        return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(message);
    }
}
