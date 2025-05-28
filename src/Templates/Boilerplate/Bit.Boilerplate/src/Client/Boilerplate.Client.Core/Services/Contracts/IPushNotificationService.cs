using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    /// <summary>
    /// Supported by the OS/Platform and allowed by the user.
    /// </summary>
    Task<bool> IsAvailable(CancellationToken cancellationToken);
    Task RequestPermission(CancellationToken cancellationToken);
    Task<PushNotificationSubscriptionDto> GetSubscription(CancellationToken cancellationToken);
    Task Subscribe(CancellationToken cancellationToken);
}
