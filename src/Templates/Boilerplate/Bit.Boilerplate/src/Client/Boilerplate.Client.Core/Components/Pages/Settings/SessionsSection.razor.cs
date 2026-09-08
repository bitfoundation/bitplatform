//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;

namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class SessionsSection
{
    private bool isLoading;
    private Guid? currentSessionId;
    private UserSessionDto? currentSession;
    private int currentPrivilegedSessionsCount;
    private int maxPrivilegedSessionsCount;
    private bool hasUnlimitedPrivilegedSessions;
    private List<Guid> revokingSessionIds = [];
    private UserSessionDto[] otherSessions = [];

    [AutoInject] private IUserController userController = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#elseif (signalR == true)
    [AutoInject] private Notification notification = default!;
    //#endif


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadSessions();
    }


    private async Task LoadSessions(bool showLoading = true)
    {
        if (showLoading)
        {
            isLoading = true;
        }

        try
        {
            var user = (await AuthenticationStateTask).User;
            currentSessionId = user.GetSessionId();

            var userSessions = await userController.GetUserSessions(CurrentCancellationToken);
            otherSessions = userSessions.Where(s => s.Id != currentSessionId).ToArray();

            currentSession = userSessions.SingleOrDefault(s => s.Id == currentSessionId);

            if (currentSession is null)
            {
                SnackBarService.Warning(Localizer[nameof(AppStrings.SessionNoLongerValidMessage)]);
            }

            maxPrivilegedSessionsCount = user.GetClaimValue<int>(AppClaimTypes.MAX_PRIVILEGED_SESSIONS);
            hasUnlimitedPrivilegedSessions = user.HasClaim(AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
                AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS.ToString(CultureInfo.InvariantCulture));
            currentPrivilegedSessionsCount = userSessions.Count(us => us.Privileged);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            if (showLoading)
            {
                isLoading = false;
            }
        }
    }

    private async Task RevokeSession(UserSessionDto session)
    {
        if (revokingSessionIds.Contains(session.Id) || session.Id == currentSessionId) return;

        revokingSessionIds.Add(session.Id);

        try
        {
            if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken))
            {
                await userController.RevokeSession(session.Id, CurrentCancellationToken);
                SnackBarService.Success(Localizer[nameof(AppStrings.RemoveSessionSuccessMessage)]);
                await LoadSessions();
            }
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            revokingSessionIds.Remove(session.Id);
        }
    }

    /// <summary>
    /// An authorized application is not a device: the fallback below guesses an operating system from the name, which
    /// made Visual Studio an Apple logo.
    /// </summary>
    private static string GetImageUrl(UserSessionDto session)
    {
        return string.IsNullOrEmpty(session.OAuthClientId) ? GetImageUrl(session.DeviceInfo) : "unknown.png";
    }

    /// <summary>What the application may do, from the scopes it was granted - the fact a user revokes on.</summary>
    private string ScopeDescription(string scope) => scope switch
    {
        OAuthScopes.DevMcp => Localizer[nameof(AppStrings.OAuthScopeDevMcpShortDescription)],
        //#if (signalR == true)
        OAuthScopes.Chat => Localizer[nameof(AppStrings.OAuthScopeChatShortDescription)],
        //#endif
        _ => scope
    };

    private static string GetImageUrl(string? deviceInfo)
    {
        if (string.IsNullOrWhiteSpace(deviceInfo)) return "unknown.png";

        var d = deviceInfo.ToLowerInvariant();

        if (d.Contains("win") /*Windows, WinUI, Win32*/) return "windows.png";

        if (d.Contains("android")) return "android.png";

        if (d.Contains("linux")) return "linux.png";

        return "apple.png";
    }

    private BitPersonaPresence GetPresence(DateTimeOffset renewedOn)
    {
        return TimeProvider.GetUtcNow() - renewedOn < TimeSpan.FromMinutes(5) ? BitPersonaPresence.Online
                    : TimeProvider.GetUtcNow() - renewedOn < TimeSpan.FromMinutes(15) ? BitPersonaPresence.Away
                    : BitPersonaPresence.Offline;
    }

    private string GetLastSeenOn(DateTimeOffset renewedOn)
    {
        return TimeProvider.GetUtcNow() - renewedOn < TimeSpan.FromMinutes(5) ? Localizer[nameof(AppStrings.Online)]
                    : TimeProvider.GetUtcNow() - renewedOn < TimeSpan.FromMinutes(15) ? Localizer[nameof(AppStrings.Recently)]
                    : TimeZoneService.ToLocalTime(renewedOn).ToString("g");
    }

    //#if (signalR == true || notification == true)
    private async Task ToggleNotification(UserSessionDto userSession)
    {
        var enabled = userSession.NotificationStatus is not UserSessionNotificationStatus.Allowed;

        if (enabled)
        {
            // User is going to allow notifications so it's an opportune time to request permission.
            // The permission might have already been requested (if userSession.NotificationStatus is UserSessionNotificationStatus.Muted), but there's no harm in asking for permission again.

            //#if (notification == true)
            if (AppPlatform.IsWindows is false)
            {
                await pushNotificationService.RequestPermission(CurrentCancellationToken);
                await pushNotificationService.Subscribe(CurrentCancellationToken);
            }
            //#else
            if (await notification.IsSupported())
            {
                await notification.RequestPermission();
            }
            //#endif
        }

        userSession.NotificationStatus = await userController.SetNotificationEnabled(userSession.Id, enabled, CurrentCancellationToken);
    }
    //#endif
}
