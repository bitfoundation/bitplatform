using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SessionsSection
{
    private bool isWaiting;
    private Guid? currentSessionId;
    private UserSessionDto? currentSession;
    private UserSessionDto[] otherSessions = [];

    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        await LoadSessions();

        await base.OnInitAsync();
    }


    private async Task LoadSessions()
    {
        List<UserSessionDto> userSessions = [];
        currentSessionId = await PrerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.GetSessionId());

        try
        {
            userSessions = await userController.GetUserSessions(CurrentCancellationToken);
        }
        finally
        {
            otherSessions = userSessions.Where(s => s.Id != currentSessionId).ToArray();
            currentSession = userSessions.Single(s => s.Id == currentSessionId);
        }
    }

    private async Task RevokeSession(UserSessionDto session)
    {
        if (isWaiting || session.Id == currentSessionId) return;

        isWaiting = true;

        try
        {
            if (await AuthManager.TryEnterPrivilegedAccessMode(CurrentCancellationToken))
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
            isWaiting = false;
        }
    }

    private static string GetImageUrl(string? deviceInfo)
    {
        if (string.IsNullOrEmpty(deviceInfo)) return "unknown.png";

        var d = deviceInfo.ToLowerInvariant();

        if (d.Contains("win") /*Windows, WinUI, Win32*/) return "windows.png";

        if (d.Contains("android")) return "android.png";

        if (d.Contains("linux")) return "linux.png";

        return "apple.png";
    }

    private BitPersonaPresence GetPresence(DateTimeOffset renewedOn)
    {
        return DateTimeOffset.UtcNow - renewedOn < TimeSpan.FromMinutes(5) ? BitPersonaPresence.Online
                    : DateTimeOffset.UtcNow - renewedOn < TimeSpan.FromMinutes(15) ? BitPersonaPresence.Away
                    : BitPersonaPresence.Offline;
    }

    private string GetLastSeenOn(DateTimeOffset renewedOn)
    {
        return DateTimeOffset.UtcNow - renewedOn < TimeSpan.FromMinutes(5) ? Localizer[nameof(AppStrings.Online)]
                    : DateTimeOffset.UtcNow - renewedOn < TimeSpan.FromMinutes(15) ? Localizer[nameof(AppStrings.Recently)]
                    : renewedOn.ToLocalTime().ToString("g");
    }
}
