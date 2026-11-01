namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

/// <summary>The Workbench copy of the app's account section; the open tab is kept in the store.</summary>
public partial class AccountSection
{
    [AutoInject] private SettingsStore store = default!;

    protected override void OnInitialized() => Subscribe(store);
}
