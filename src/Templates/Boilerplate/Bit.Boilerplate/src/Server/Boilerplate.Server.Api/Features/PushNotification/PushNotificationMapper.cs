using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// More info at src/Server/Boilerplate.Server.Api/Features/Mappers.md
/// </summary>
[Mapper]
public static partial class PushNotificationMapper
{
    public static partial void Patch(this PushNotificationSubscriptionDto source, PushNotificationSubscription destination);
}
