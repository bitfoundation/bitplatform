namespace Boilerplate.Workbench.Components.Chrome;

[BlexStore(Name = "chrome", Persist = true)]
public partial class ChromeStore
{
    [BlexState] private bool _isNavPanelOpen;
    [BlexState] private bool _isNavPanelToggled;
    [BlexState(Persist = false)] private bool _isMenuOpen;
    [BlexState] private int _activeUserIndex;
    [BlexState] private string _tenantKey = WorkbenchDemoUsers.TenantKeys[0];
    [BlexState] private string? _accent;

    [BlexAction] private void OnOpenNavPanel(bool isOpen) => IsNavPanelOpen = isOpen;

    [BlexAction] private void OnToggleNavPanel(bool isToggled) => IsNavPanelToggled = isToggled;

    [BlexAction] private void OnOpenMenu(bool isOpen) => IsMenuOpen = isOpen;

    [BlexAction] private void OnSwitchAccount(int index) => ActiveUserIndex = index;

    [BlexAction] private void OnSwitchTenant(string tenantKey) => TenantKey = tenantKey;

    [BlexAction] private void OnPickAccent(string? accent) => Accent = accent;
}
