//+:cnd:noEmit
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
}
