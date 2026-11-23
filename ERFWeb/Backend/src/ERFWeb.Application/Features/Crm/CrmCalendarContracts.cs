namespace ERFWeb.Application.Features.Crm;

/// <summary>
/// One CRM activity on a day of the claimant's calendar. <see cref="Completed"/> is false for Open and Cancelled
/// ones. <see cref="Role"/> is Lead or Companion; <see cref="WorkWith"/> names the other people on it (ERFWeb names
/// where linked). <see cref="ClaimedOnThisReport"/> is true when a line of the report under review uses it.
/// <see cref="IsLeave"/> marks a whole-day leave or other activity from the CRM calendar (VL, SL, Holiday, ...; the
/// type is <see cref="ActivityType"/>, the text <see cref="Subject"/>).
/// </summary>
public sealed record CrmCalendarEntryDto(
    string ActivityId,
    string? ActivityType,
    string? Status,
    bool Completed,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    string? Subject,
    string? ServiceNumber,
    string? CustomerName,
    string? InstrumentName,
    string? SerialNumber,
    string Role,
    string? WorkWith,
    bool ClaimedOnThisReport,
    bool IsLeave);

/// <summary>
/// One day of the report period: the claimant's CRM activities and how many of the report's lines fall on it.
/// <see cref="TimeOff"/> is true when the CRM has the claimant on vacation leave, sick leave or a holiday that day.
/// </summary>
public sealed record CrmCalendarDayDto(DateOnly Date, int ExpenseLineCount, IReadOnlyList<CrmCalendarEntryDto> Entries, bool TimeOff);

/// <summary>
/// The claimant's CRM calendar over the report period. <see cref="Linked"/> is false when the claimant's account has
/// no CRM employee link (then <see cref="Days"/> is empty).
/// </summary>
public sealed record CrmCalendarDto(bool Linked, string EmployeeName, DateOnly? DateFrom, DateOnly? DateTo, IReadOnlyList<CrmCalendarDayDto> Days);

public interface ICrmCalendarService
{
    /// <summary>
    /// The CRM activities of the report's claimant on each day of the report period, for a signatory who may view
    /// the report. 404 for an unknown report, 403 when the caller may not view it, 503 when the CRM cannot answer.
    /// </summary>
    Task<CrmCalendarDto> GetForReportAsync(Guid reportId, CancellationToken ct = default);
}
