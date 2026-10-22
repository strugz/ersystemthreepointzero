using ERFWeb.Domain;

namespace ERFWeb.Application.Common;

/// <summary>Wall-clock abstraction so time-dependent logic is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Can the API reach its database right now? Used by the anonymous health endpoint.</summary>
public interface IDatabaseHealthCheck
{
    /// <summary>False when the database does not answer within a few seconds; never throws.</summary>
    Task<bool> CanConnectAsync(CancellationToken ct = default);
}

/// <summary>The authenticated caller, resolved from the request's claims.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    long UserId { get; }
    string Username { get; }
    string FullName { get; }
    IReadOnlyCollection<UserRole> Roles { get; }
    bool IsInRole(UserRole role);
}

/// <summary>
/// Runs a unit of work inside a serializable transaction, committing on success
/// and rolling back on any exception.
/// </summary>
public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}

/// <summary>A durable, security-relevant workflow event to record in the audit trail.</summary>
public sealed record WorkflowAuditEntry(
    string Event,
    Guid? ReportId,
    long? ActorUserId,
    string? Detail);

/// <summary>Writes workflow audit rows within the current transaction.</summary>
public interface IWorkflowAuditWriter
{
    Task WriteAsync(WorkflowAuditEntry entry, CancellationToken ct = default);
}
