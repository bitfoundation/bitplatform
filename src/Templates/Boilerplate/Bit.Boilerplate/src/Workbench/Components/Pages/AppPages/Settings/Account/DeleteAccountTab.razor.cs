namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

public partial class DeleteAccountTab
{
    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public ChromeShell? Shell { get; set; }

    protected override void OnInitialized() => Subscribe(store);

    private async Task DeleteAccount()
    {
        store.OpenDeleteDialog(false);

        await ScenarioState.Save(CurrentCancellationToken);

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
