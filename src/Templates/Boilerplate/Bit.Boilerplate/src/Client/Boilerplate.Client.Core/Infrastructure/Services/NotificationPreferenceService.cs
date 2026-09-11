namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// The device's notifications choice from AppMenu's switch, covering push and SignalR's in-app messages alike. Off
/// until the user turns it on. It lives on the device rather than the user session, so every UpdateSession reports it
/// (See AppClientCoordinator) and PushNotificationServiceBase only subscribes while it is on.
/// </summary>
public partial class NotificationPreferenceService
{
    private const string NotificationsEnabledStoreKey = "NotificationsEnabled";

    [AutoInject] private IStorageService storageService = default!;

    public async Task<bool> IsEnabled() => await storageService.GetItem(NotificationsEnabledStoreKey) is "true";

    public async Task<UserSessionNotificationStatus> GetSessionStatus() => await IsEnabled()
        ? UserSessionNotificationStatus.Allowed
        : UserSessionNotificationStatus.Muted;

    public async Task SetEnabled(bool enabled)
    {
        if (enabled)
        {
            await storageService.SetItem(NotificationsEnabledStoreKey, "true", persistent: true);
        }
        else
        {
            await storageService.RemoveItem(NotificationsEnabledStoreKey);
        }
    }
}
