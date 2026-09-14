namespace ERFWeb.Application.Features.Expenses;

/// <summary>
/// Issues the next ERF reference number in the yearly series. Must be called inside the
/// workflow transaction so the counter increment commits or rolls back with the filing.
/// </summary>
public interface IReferenceNumberGenerator
{
    Task<string> NextAsync(DateTimeOffset filedAt, CancellationToken ct = default);
}
