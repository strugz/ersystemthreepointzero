using System.Security.Claims;
using System.Data.Common;

namespace ERSystem.Web.Application.Common;

public enum SortDirection
{
    Ascending,
    Descending
}

public record PagedRequest
{
    private int _page = 1;
    private int _pageSize = 25;

    public int Page { get => _page; init => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; init => _pageSize = Math.Clamp(value, 1, 100); }
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string Username { get; }
    IReadOnlyCollection<string> Roles { get; }
    ClaimsPrincipal Principal { get; }
}

public interface IRowVersionCodec
{
    string Encode(byte[]? value);
    byte[] Decode(string value);
    bool Matches(byte[]? current, string expected);
}

public sealed record WorkflowAuditEntry(
    string ReportId, int ActorUserId, string EventType, string? PreviousState,
    string? NewState, string? Remarks, DateTime OccurredAtUtc, Guid CorrelationId);

public interface IWorkflowAuditWriter
{
    Task WriteAsync(DbConnection connection, DbTransaction transaction, WorkflowAuditEntry entry, CancellationToken cancellationToken);
}

public interface IReportAuthorizationService
{
    Task EnsureManagerCanAccessAsync(int managerUserId, string reportId, CancellationToken cancellationToken);
}

public interface ITransactionRunner
{
    Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}

public static class TextNormalization
{
    public static string NormalizeUsername(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    public static string? NormalizeOptionalText(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    public static string TrimLegacyFixedLengthText(string? value) => value?.Trim() ?? string.Empty;

    public static string? NormalizeOptional(string? value, int maximumLength) => NormalizeOptionalText(value, maximumLength);
    public static string TrimLegacy(string? value) => TrimLegacyFixedLengthText(value);
}

public class AppException(string message) : Exception(message);
public sealed class NotFoundException(string message) : AppException(message);
public sealed class ForbiddenException(string message) : AppException(message);
public sealed class ConflictException(string message) : AppException(message);
public sealed class ValidationException(string message) : AppException(message);
