namespace Boilerplate.Workbench.Components.Chrome;

/// <summary>
/// The app's account menu in a view: accounts, language, tenant, time zone, notifications, theme and accent, run on
/// sample data and the view's scenario instead of the app's services.
/// </summary>
public partial class ChromeAppMenu
{
    private bool isDark;
    private bool showCultures;
    private bool showTenants;
    private bool isSignOutConfirmOpen;
    private BitChoiceGroupItem<string>[] cultures = [];
    private BitChoiceGroupItem<string>[] tenants = [];

    [AutoInject] private ChromeStore chromeStore = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    /// <summary>The shell around the menu, which owns the sign-in state and the account actions.</summary>
    [CascadingParameter] public ChromeShell Shell { get; set; } = default!;

    private string BackIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChromeBackMirrored : BitIconName.ChromeBack;

    private string ChevronIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChevronLeft : BitIconName.ChevronRight;

    private string ThemeToggleLabel => isDark
        ? AppLocalizer[nameof(AppStrings.SwitchToLightThemeTitle)]
        : AppLocalizer[nameof(AppStrings.SwitchToDarkThemeTitle)];

    /// <summary>The menu button's accessible name: the signed-in user's name, or the account menu's title.</summary>
    private string AccountMenuLabel => Shell.IsSignedIn
        ? Localizer[Shell.ActiveUser.NameKey]
        : AppLocalizer[nameof(AppStrings.AccountMenuTitle)];

    protected override void OnInitialized()
    {
        isDark = Scenario.Theme is WorkbenchThemeMode.Dark;

        cultures = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];

        tenants = [.. WorkbenchDemoUsers.TenantKeys.Select(key => new BitChoiceGroupItem<string> { Value = key, Text = Localizer[key] })];

        Subscribe(chromeStore);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The System theme follows the device, which only the browser knows, so the applied theme is read from the page.
        if (firstRender is false || Scenario.Theme is not WorkbenchThemeMode.System)
            return;

        isDark = (await bitThemeManager.GetCurrentThemeAsync())?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;

        StateHasChanged();
    }

    private Task SignIn()
    {
        chromeStore.OpenMenu(false);

        return ShowSignInNote();
    }

    private void AskToSignOut()
    {
        chromeStore.OpenMenu(false);

        isSignOutConfirmOpen = true;
    }

    private Task SignOut()
    {
        isSignOutConfirmOpen = false;

        return Shell.SignOut();
    }

    /// <summary>Brings the menu back to its main list when it closes.</summary>
    private void ResetMenu()
    {
        showCultures = false;
        showTenants = false;
    }

    /// <summary>Toggles the view's theme in place and records it in the scenario, so the shell and screenshots follow.</summary>
    private async Task ToggleTheme()
    {
        var theme = await bitThemeManager.ToggleDarkLightAsync();
        isDark = theme?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;

        await ReportScenario(Scenario with { Theme = isDark ? WorkbenchThemeMode.Dark : WorkbenchThemeMode.Light });
    }

    /// <summary>Reloads the view in the chosen language; the default culture is left out of the address.</summary>
    private async Task ChangeCulture(string? culture)
    {
        chromeStore.OpenMenu(false);

        await ReportScenario(Scenario with { Culture = culture == CultureInfoManager.DefaultCulture.Name ? null : culture }, isApplied: false);
    }
}
