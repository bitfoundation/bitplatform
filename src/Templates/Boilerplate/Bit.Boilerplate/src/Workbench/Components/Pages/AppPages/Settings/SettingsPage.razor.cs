namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public partial class SettingsPage
{
    [AutoInject] private SettingsStore store = default!;

    public SettingsProfile? Profile { get; private set; }

    public bool IsProfileLoading { get; private set; } = true;

    public bool IsProfileFailed { get; private set; }

    public event Action? ProfileChanged;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await LoadProfile();
    }

    public WorkbenchDemoUser ActiveUser => WorkbenchDemoUsers.Find(Scenario.User);

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

    public void UpdateProfile(SettingsProfile profile)
    {
        Profile = profile;
        store.SaveProfile(profile);
        ProfileChanged?.Invoke();
    }
}
