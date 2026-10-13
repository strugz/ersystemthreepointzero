using ERFWeb.Application.Features.Entitlements;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Entitlements;

/// <summary>
/// Placeholder for the before/after emails of spec 9.14. Like the filing notifier, it only logs
/// until SMTP delivery lands; the audit row written with each change already records the values.
/// </summary>
public sealed class LoggingEntitlementChangeNotifier(ILogger<LoggingEntitlementChangeNotifier> logger) : IEntitlementChangeNotifier
{
    public Task NotifyAsync(EntitlementChangeNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        logger.LogInformation(
            "Entitlement change {Kind} by user {ActorUserId}: {Summary}. {AffectedCount} affected employee(s) to notify.",
            notification.Kind, notification.ActorUserId, notification.Summary, notification.AffectedUserIds.Count);
        return Task.CompletedTask;
    }
}
