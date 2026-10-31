namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeAppMenu
{
    private bool isDark;
    private bool showCultures;
    private bool showTenants;
    private BitChoiceGroupItem<string>[] cultures = [];
    private BitChoiceGroupItem<string>[] tenants = [];

    [AutoInject] private ChromeStore chromeStore = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    [CascadingParameter] public ChromeShell Shell { get; set; } = default!;

    private string BackIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChromeBackMirrored : BitIconName.ChromeBack;

    private string ChevronIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChevronLeft : BitIconName.ChevronRight;

    protected override void OnInitialized()
    {
        isDark = Scenario.Theme is WorkbenchThemeMode.Dark;

        cultures = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];

        tenants = [.. WorkbenchDemoUsers.TenantKeys.Select(key => new BitChoiceGroupItem<string> { Value = key, Text = Localizer[key] })];

        Subscribe(chromeStore);
    }

    private Task SignIn()
    {
        chromeStore.OpenMenu(false);

        return Shell.SignIn();
    }

    private Task SignOut()
    {
        chromeStore.OpenMenu(false);

        return Shell.SignOut();
    }

    private void ResetMenu()
    {
        showCultures = false;
        showTenants = false;
    }

    private async Task ToggleTheme()
    {
        var theme = await bitThemeManager.ToggleDarkLightAsync();
        isDark = theme?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;

        await ReportScenario(Scenario with { Theme = isDark ? WorkbenchThemeMode.Dark : WorkbenchThemeMode.Light });
    }

    private async Task ChangeCulture(string? culture)
    {
        chromeStore.OpenMenu(false);

        await ReportScenario(Scenario with { Culture = culture == CultureInfoManager.DefaultCulture.Name ? null : culture }, isApplied: false);
    }
}
