namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

public partial class PasswordlessTab
{
    [AutoInject] private SettingsStore store = default!;

    protected override void OnInitialized() => Subscribe(store);

    private async Task EnablePasswordless()
    {
        await ScenarioState.Save(CurrentCancellationToken);

        store.SetPasswordless(true);
    }

    private async Task DisablePasswordless()
    {
        await ScenarioState.Save(CurrentCancellationToken);

        store.SetPasswordless(false);
    }
}
