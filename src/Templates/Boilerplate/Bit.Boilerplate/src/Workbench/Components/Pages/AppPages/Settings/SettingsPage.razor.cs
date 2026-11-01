namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>
/// The Workbench copy of the app's settings page. It loads the profile through the scenario state and hands itself to
/// its sections as a cascading value, so they share the profile and its loading state.
/// </summary>
public partial class SettingsPage
{
    [AutoInject] private SettingsStore store = default!;

    public SettingsProfile? Profile { get; private set; }

    public bool IsProfileLoading { get; private set; } = true;

    public bool IsProfileFailed { get; private set; }

    /// <summary>Raised when the profile or its loading state changes.</summary>
    public event Action? ProfileChanged;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await LoadProfile();
    }

    /// <summary>The demo user the view is signed in as.</summary>
    public WorkbenchDemoUser ActiveUser => WorkbenchDemoUsers.Find(Scenario.User);

    /// <summary>Loads the user's saved profile, or the demo user's sample one.</summary>
    public async Task LoadProfile()
    {
        IsProfileLoading = true;
        IsProfileFailed = false;
        ProfileChanged?.Invoke();

        try
        {
            var user = ActiveUser;

            Profile = await ScenarioState.Load("settings.profile",
                                               () => store.Profile ?? SettingsDemoData.CreateProfile(Localizer[user.NameKey], user.Email),
                                               () => SettingsDemoData.CreateEmptyProfile(user.Email),
                                               CurrentCancellationToken);
        }
        catch (WorkbenchSimulatedException)
        {
            IsProfileFailed = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        IsProfileLoading = false;
        ProfileChanged?.Invoke();
        StateHasChanged();
    }

    /// <summary>Applies a profile a section saved and keeps it in the store.</summary>
    public void UpdateProfile(SettingsProfile profile)
    {
        Profile = profile;
        store.SaveProfile(profile);
        ProfileChanged?.Invoke();
    }
}
