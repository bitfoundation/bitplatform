using Riok.Mapperly.Abstractions;

namespace Boilerplate.Server.Api.Features.PushNotification;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class PushNotificationMapper
{
    public static partial void Patch(this PushNotificationSubscriptionDto source, PushNotificationSubscription destination);
}
