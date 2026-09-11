//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;

namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// Session presentation, shared by <c>SessionsSection</c> (the user's own list) and <c>UsersPage</c> (the admin's).
/// </summary>
public static class UserSessionDtoExtensions
{
    private const string OsImagesUrl = "/_content/Boilerplate.Client.Core/images/os";

    extension(UserSessionDto session)
    {
        /// <summary>An authorized application is not a device, so it never reaches the OS guess below.</summary>
        public string ImageUrl => string.IsNullOrEmpty(session.OAuthClientId)
                                    ? $"{OsImagesUrl}/{GetOsImage(session.DeviceInfo)}"
                                    : $"{OsImagesUrl}/oauth.png";

        /// <summary>Where the session comes from, falling back to its IP when no address was resolved for it.</summary>
        public string? DisplayAddress => string.IsNullOrEmpty(session.Address) ? session.IP : session.Address;

        /// <summary>The IP as a detail under <c>DisplayAddress</c>, so it is never shown twice.</summary>
        public string? DisplayIP => string.IsNullOrEmpty(session.Address) ? null : session.IP;

        public string GetLastSeenOn(DateTimeOffset utcNow, IStringLocalizer<AppStrings> localizer, TimeZoneService timeZoneService)
        {
            return utcNow - session.RenewedOnDateTimeOffset < TimeSpan.FromMinutes(5) ? localizer[nameof(AppStrings.Online)]
                        : utcNow - session.RenewedOnDateTimeOffset < TimeSpan.FromMinutes(15) ? localizer[nameof(AppStrings.Recently)]
                        : timeZoneService.ToLocalTime(session.RenewedOnDateTimeOffset).ToString("g");
        }

        public BitPersonaPresence GetPresence(DateTimeOffset utcNow)
        {
            return utcNow - session.RenewedOnDateTimeOffset < TimeSpan.FromMinutes(5) ? BitPersonaPresence.Online
                        : utcNow - session.RenewedOnDateTimeOffset < TimeSpan.FromMinutes(15) ? BitPersonaPresence.Away
                        : BitPersonaPresence.Offline;
        }

        //#if (signalR == true || notification == true)
        /// <summary>Shown read-only: only the device itself changes it, through AppMenu's switch.</summary>
        public string GetNotificationStatusTitle(IStringLocalizer<AppStrings> localizer)
        {
            return session.NotificationStatus is UserSessionNotificationStatus.Allowed
                ? localizer[nameof(AppStrings.SessionNotificationsAllowedTitle)]
                : localizer[nameof(AppStrings.SessionNotificationsMutedTitle)];
        }
        //#endif

        /// <summary>What the authorized application may do, from the scopes it was granted.</summary>
        public IEnumerable<string> ScopeDescriptions(IStringLocalizer<AppStrings> localizer)
        {
            return (session.OAuthScope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(scope => scope switch
            {
                OAuthScopes.DevMcp => localizer[nameof(AppStrings.OAuthScopeDevMcpShortDescription)].ToString(),
                //#if (signalR == true)
                OAuthScopes.Chat => localizer[nameof(AppStrings.OAuthScopeChatShortDescription)].ToString(),
                //#endif
                _ => scope
            });
        }
    }

    private static string GetOsImage(string? deviceInfo)
    {
        if (string.IsNullOrWhiteSpace(deviceInfo)) return "unknown.png";

        var d = deviceInfo.ToLowerInvariant();

        if (d.Contains("win") /*Windows, WinUI, Win32*/) return "windows.png";

        if (d.Contains("android")) return "android.png";

        if (d.Contains("linux")) return "linux.png";

        return "apple.png";
    }
}
