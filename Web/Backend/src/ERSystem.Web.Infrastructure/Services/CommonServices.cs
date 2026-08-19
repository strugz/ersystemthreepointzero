using System.Security.Claims;
using ERSystem.Web.Application.Common;
using Microsoft.AspNetCore.Http;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public int UserId => int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : 0;
    public string Username => Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public IReadOnlyCollection<string> Roles => Principal.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray();
}

public sealed class RowVersionCodec : IRowVersionCodec
{
    public string Encode(byte[]? value) => value is { Length: > 0 } ? Convert.ToBase64String(value) : string.Empty;

    public byte[] Decode(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException exception)
        {
            throw new ValidationException("The row version is invalid.") { Source = exception.Source };
        }
    }

    public bool Matches(byte[]? current, string expected)
    {
        if (current is not { Length: > 0 } || string.IsNullOrWhiteSpace(expected)) return false;
        var supplied = Decode(expected);
        return supplied.AsSpan().SequenceEqual(current);
    }
}
