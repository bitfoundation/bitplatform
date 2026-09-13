// [mirror] not implemented push notification service of the windows targets - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/Windows/Services/WindowsPushNotificationService.cs

using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public override Task RequestPermission(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
