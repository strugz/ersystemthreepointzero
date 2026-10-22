namespace ERFWeb.Domain.Finance;

/// <summary>How long an approved report has waited in the Finance queue since final approval.</summary>
public enum AgingBucket
{
    Days0To7,
    Days8To15,
    Days16To30,
    Days31To60,
    Over60Days,
}

/// <summary>
/// Finance aging (plan Phase 7): open queue items grouped by whole days since final approval, so
/// Finance can see which receipts are overdue. Buckets are inclusive on both ends.
/// </summary>
public static class FinanceAging
{
    public static readonly IReadOnlyList<AgingBucket> Buckets = Enum.GetValues<AgingBucket>();

    public static AgingBucket BucketFor(int daysSinceApproval) => daysSinceApproval switch
    {
        <= 7 => AgingBucket.Days0To7,
        <= 15 => AgingBucket.Days8To15,
        <= 30 => AgingBucket.Days16To30,
        <= 60 => AgingBucket.Days31To60,
        _ => AgingBucket.Over60Days,
    };

    public static string Label(AgingBucket bucket) => bucket switch
    {
        AgingBucket.Days0To7 => "0-7 days",
        AgingBucket.Days8To15 => "8-15 days",
        AgingBucket.Days16To30 => "16-30 days",
        AgingBucket.Days31To60 => "31-60 days",
        _ => "Over 60 days",
    };
}
