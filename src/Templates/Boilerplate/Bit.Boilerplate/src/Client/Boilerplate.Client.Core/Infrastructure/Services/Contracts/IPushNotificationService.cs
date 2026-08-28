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
    Task Subscribe(CancellationToken cancellationToken);
    Task Unsubscribe(CancellationToken cancellationToken);
    /// <summary>
    /// The device-stored preference the user controls through AppMenu's push notifications toggle, honored whether
    /// the user is signed in or not. <see cref="Subscribe"/> respects it, so the automatic re-subscribe on every
    /// auth-state change cannot undo an opt-out.
    /// </summary>
    Task<bool> IsEnabled();
    Task SetEnabled(bool enabled, CancellationToken cancellationToken);
}
