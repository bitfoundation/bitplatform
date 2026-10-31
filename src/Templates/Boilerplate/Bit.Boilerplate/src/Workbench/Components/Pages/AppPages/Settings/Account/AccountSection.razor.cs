namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

public partial class AccountSection
{
    [AutoInject] private SettingsStore store = default!;

    protected override void OnInitialized() => Subscribe(store);
}
