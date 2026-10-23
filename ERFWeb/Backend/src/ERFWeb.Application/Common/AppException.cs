namespace ERFWeb.Application.Common;

/// <summary>
/// Base for expected application errors. Each subtype carries a stable machine
/// <see cref="Code"/> so the API can map it to the right HTTP status + ProblemDetails.
/// </summary>
public abstract class AppException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Requested resource does not exist. Maps to 404.</summary>
public sealed class NotFoundException(string message = "The requested resource was not found.")
    : AppException(message, "not_found");

/// <summary>Caller is authenticated but not allowed to perform the action. Maps to 403.</summary>
public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException(message, "forbidden");

/// <summary>Stale, duplicate, or out-of-order action. Maps to 409.</summary>
public sealed class ConflictException(string message = "The action conflicts with the current state.")
    : AppException(message, "conflict");

/// <summary>An outside system the action needs is not set up or not answering. Maps to 503.</summary>
public sealed class ServiceUnavailableException(string message, string code = "service_unavailable")
    : AppException(message, code);

/// <summary>Input failed validation. Maps to 400 with per-field errors.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "validation_failed")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
