namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

/// <summary>The Workbench copy of the app's delete-account tab; deleting signs the view out.</summary>
public partial class DeleteAccountTab
{
    [AutoInject] private SettingsStore store = default!;

    /// <summary>The app's chrome, when the view shows it; it signs out the way its account menu does.</summary>
    [CascadingParameter] public ChromeShell? Shell { get; set; }

    protected override void OnInitialized() => Subscribe(store);

    private async Task DeleteAccount()
    {
        store.OpenDeleteDialog(false);

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (Shell is null)
        {
            await ReportScenario(Scenario with { Auth = WorkbenchAuthState.SignedOut });
        }
        else
        {
            await Shell.SignOut();
        }
    }
}
