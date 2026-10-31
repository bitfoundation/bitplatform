//+:cnd:noEmit
namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public partial class SessionsSection
{
    private const int MaxPrivilegedSessionsCount = 3;

    private bool isFailed;
    private bool isLoading = true;
    private SettingsSession? currentSession;
    private SettingsSession[] otherSessions = [];
    private SettingsSession[] sessions = [];
    private readonly List<Guid> revokingSessionIds = [];

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    private int PrivilegedSessionsCount => sessions.Count(s => s.IsPrivileged);

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await LoadSessions();
    }

    private async Task LoadSessions()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var all = await ScenarioState.Load("settings.sessions",
                                               () => SettingsDemoData.CreateSessions(now).Where(s => store.RevokedSessionIds.Contains(s.Id) is false).ToArray(),
                                               () => SettingsDemoData.CreateSessions(now).Take(1).ToArray(),
                                               CurrentCancellationToken);

            sessions = all;
            currentSession = all.SingleOrDefault(s => s.Id == SettingsDemoData.CurrentSessionId);
            otherSessions = [.. all.Where(s => s.Id != SettingsDemoData.CurrentSessionId)];
            isLoading = false;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
            isLoading = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RevokeSession(SettingsSession session)
    {
        if (revokingSessionIds.Contains(session.Id) || session.Id == SettingsDemoData.CurrentSessionId)
            return;

        revokingSessionIds.Add(session.Id);

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            store.RevokeSession(session.Id);

            SnackBarService.Success(AppLocalizer[nameof(AppStrings.RemoveSessionSuccessMessage)]);
            await LoadSessions();
        }
        finally
        {
            revokingSessionIds.Remove(session.Id);
        }
    }

    private string GetLastSeenOn(SettingsSession session)
    {
        var age = DateTimeOffset.UtcNow - session.LastSeen;

        return age < TimeSpan.FromMinutes(5) ? AppLocalizer[nameof(AppStrings.Online)]
             : age < TimeSpan.FromMinutes(15) ? AppLocalizer[nameof(AppStrings.Recently)]
             : session.LastSeen.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }

    private static BitPersonaPresence GetPresence(SettingsSession session)
    {
        var age = DateTimeOffset.UtcNow - session.LastSeen;

        return age < TimeSpan.FromMinutes(5) ? BitPersonaPresence.Online
             : age < TimeSpan.FromMinutes(15) ? BitPersonaPresence.Away
             : BitPersonaPresence.Offline;
    }

    //#if (signalR == true || notification == true)
    private string GetNotificationStatusTitle(SettingsSession session)
    {
        return session.IsNotificationAllowed
            ? AppLocalizer[nameof(AppStrings.SessionNotificationsAllowedTitle)]
            : AppLocalizer[nameof(AppStrings.SessionNotificationsMutedTitle)];
    }
    //#endif
}
