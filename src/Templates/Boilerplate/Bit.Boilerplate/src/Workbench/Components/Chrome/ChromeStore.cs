namespace Boilerplate.Workbench.Components.Chrome;

[BlexStore(Name = "chrome", Persist = true)]
public partial class ChromeStore
{
    [BlexState] private bool _isNavPanelOpen;
    [BlexState] private bool _isNavPanelToggled;
    [BlexState(Persist = false)] private bool _isMenuOpen;
    [BlexState] private string _tenantKey = WorkbenchDemoUsers.TenantKeys[0];
    [BlexState] private string? _accent;
    [BlexState] private string? _timeZoneId;
    [BlexState] private bool _areNotificationsOn = true;

    [BlexAction] private void OnOpenNavPanel(bool isOpen) => IsNavPanelOpen = isOpen;

    [BlexAction] private void OnToggleNavPanel(bool isToggled) => IsNavPanelToggled = isToggled;

    [BlexAction] private void OnOpenMenu(bool isOpen) => IsMenuOpen = isOpen;

    [BlexAction] private void OnSwitchTenant(string tenantKey) => TenantKey = tenantKey;

    [BlexAction] private void OnPickAccent(string? accent) => Accent = accent;

    [BlexAction] private void OnPickTimeZone(string? timeZoneId) => TimeZoneId = timeZoneId;

    [BlexAction] private void OnTurnNotifications(bool isOn) => AreNotificationsOn = isOn;
}
