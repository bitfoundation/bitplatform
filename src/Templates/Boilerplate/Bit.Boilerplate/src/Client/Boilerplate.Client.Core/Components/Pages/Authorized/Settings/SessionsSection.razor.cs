using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SessionsSection
{
    private bool isLoading;
    private Guid? currentSessionId;
    private bool hasRevokedAnySession;
    private UserSessionDto? currentSession;
    private List<Guid> revokingSessionIds = [];
    private UserSessionDto[] otherSessions = [];

    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        await LoadSessions();

        await base.OnInitAsync();
    }


    private async Task LoadSessions(bool showLoading = true)
    {
        if (showLoading)
        {
            isLoading = true;
        }

        try
        {
            currentSessionId = await PrerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.GetSessionId());

            var userSessions = await userController.GetUserSessions(CurrentCancellationToken);
            otherSessions = userSessions.Where(s => s.Id != currentSessionId).ToArray();
            currentSession = userSessions.Single(s => s.Id == currentSessionId);
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
                hasRevokedAnySession = true;
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

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (hasRevokedAnySession && await AuthorizationService.AuthorizeAsync((await AuthenticationStateTask).User, AuthPolicies.PRIVILEGED_ACCESS) is { Succeeded: false })
        {
            // Refreshing the token to check if the user session can now be privileged.
            await AuthManager.RefreshToken("CheckPrivilege");
        }
    }
}
