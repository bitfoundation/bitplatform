namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

/// <summary>The Workbench copy of the app's passwordless tab; enabling and disabling only change the store.</summary>
public partial class PasswordlessTab
{
    [AutoInject] private SettingsStore store = default!;

    protected override void OnInitialized() => Subscribe(store);

    private Task EnablePasswordless() => SetPasswordless(true);

    private Task DisablePasswordless() => SetPasswordless(false);

    /// <summary>Waits as a request would, then keeps the choice; nothing changes when the tab closes meanwhile.</summary>
    private async Task SetPasswordless(bool isEnabled)
    {
        try
        {
            await ScenarioState.Save(CurrentCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        store.SetPasswordless(isEnabled);
    }
}
