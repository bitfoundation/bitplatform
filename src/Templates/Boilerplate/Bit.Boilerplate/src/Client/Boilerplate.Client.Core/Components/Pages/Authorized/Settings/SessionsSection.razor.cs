using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SessionsSection
{
    private bool isWaiting;
    private Guid? currentSessionId;
    private UserSessionDto? currentSession;
    private IEnumerable<UserSessionDto> otherSessions = [];


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
            otherSessions = userSessions.Where(s => s.SessionUniqueId != currentSessionId);
            currentSession = userSessions.SingleOrDefault(s => s.SessionUniqueId == currentSessionId);
        }
    }

    private async Task RevokeSession(UserSessionDto session)
    {
        if (isWaiting || session.SessionUniqueId == currentSessionId) return;

        isWaiting = true;

        try
        {
            await userController.RevokeSession(session.SessionUniqueId, CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.RemoveSessionSuccessMessage)]);
            await LoadSessions();
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

    private static string GetImageUrl(string? device)
    {
        var d = device?.ToLower() ?? "";

        if (d.Contains("windows")) return "windows.png";

        if (d.Contains("android")) return "android.png";

        if (d.Contains("linux")) return "linux.png";

        return "apple.png";
    }

    private static BitPersonaPresence GetPresence(string? lastSeenOn)
    {
        return lastSeenOn == AppStrings.Online ? BitPersonaPresence.Online
             : lastSeenOn == AppStrings.Recently ? BitPersonaPresence.Away
             : BitPersonaPresence.Offline;
    }
}
