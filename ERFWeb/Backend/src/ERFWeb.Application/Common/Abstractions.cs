using ERFWeb.Domain;

namespace ERFWeb.Application.Common;

/// <summary>Wall-clock abstraction so time-dependent logic is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Can the API use its database right now? Used by the anonymous health endpoint.</summary>
public interface IDatabaseHealthCheck
{
    /// <summary>Never throws: an unreachable database or one missing a required script is a state, not an error.</summary>
    Task<DatabaseHealth> CheckAsync(CancellationToken ct = default);
}

public enum DatabaseState
{
    Ok,

    /// <summary>No answer within a few seconds.</summary>
    Unreachable,

    /// <summary>Reachable, but a <c>Database/NNN_*.sql</c> script this API needs has not been run.</summary>
    SchemaOutdated,
}

/// <param name="SchemaVersion">Highest script number recorded in <c>schema_versions</c>; null when unreachable.</param>
/// <param name="RequiredSchemaVersion">The script number this build of the API needs.</param>
public sealed record DatabaseHealth(DatabaseState State, int? SchemaVersion, int RequiredSchemaVersion);

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
