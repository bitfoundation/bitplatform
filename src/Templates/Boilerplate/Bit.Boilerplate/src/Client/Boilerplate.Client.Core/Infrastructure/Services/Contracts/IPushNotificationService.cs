using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Core.Infrastructure.Services.Contracts;

public interface IPushNotificationService
{
    string? Token { get; set; }
    /// <summary>
    /// Supported by the OS/Platform and allowed by the user.
    /// </summary>
    Task<bool> IsAvailable(CancellationToken cancellationToken);
    Task RequestPermission(CancellationToken cancellationToken);
    Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    /// <summary>
    /// A no-op unless the device has opted in (See NotificationPreferenceService), so the automatic subscribe on every
    /// auth-state change never subscribes a device that has not.
    /// </summary>
    Task Subscribe(CancellationToken cancellationToken);
    Task Unsubscribe(CancellationToken cancellationToken);
}
