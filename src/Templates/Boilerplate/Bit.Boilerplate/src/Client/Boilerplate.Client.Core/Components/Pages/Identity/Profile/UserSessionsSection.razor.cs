using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class UserSessionsSection
{
    private bool isLoading;
    private bool isWaiting;
    private string? message;
    private Guid? currentSessionId;
    private UserSessionDto? currentSession;
    private ElementReference messageRef = default!;
    private BitColor messageColor = BitColor.Error;
    private IEnumerable<UserSessionDto> otherSessions = [];

    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        await LoadSessions();

        await base.OnInitAsync();
    }

    private async Task LoadSessions()
    {
        isLoading = true;
        List<UserSessionDto> userSessions = [];
        currentSessionId = await PrerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.GetSessionId());

        try
        {
            userSessions = await userController.GetUserSessions(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
            otherSessions = userSessions.Where(s => s.SessionUniqueId != currentSessionId);
            currentSession = userSessions.SingleOrDefault(s => s.SessionUniqueId == currentSessionId);
        }
    }

    private async Task RevokeSession(UserSessionDto session)
    {
        if (isWaiting || session.SessionUniqueId == currentSessionId) return;

        isWaiting = true;
        message = null;

        try
        {
            await userController.RevokeSession(session.SessionUniqueId, CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.RemoveSessionSuccessMessage)];
            messageColor = BitColor.Success;

            await LoadSessions();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
        }
        finally
        {
            isWaiting = false;
            await messageRef.ScrollIntoView();
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
