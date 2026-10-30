namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeAppMenu
{
    private bool isOpen;
    private bool isDark;
    private bool showCultures;
    private bool showTenants;
    private BitChoiceGroupItem<string>[] cultures = [];
    private BitChoiceGroupItem<string>[] tenants = [];

    [AutoInject] private BitThemeManager bitThemeManager = default!;

    [CascadingParameter] public ChromeShell Shell { get; set; } = default!;

    private string BackIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChromeBackMirrored : BitIconName.ChromeBack;

    private string ChevronIconName => CurrentDir is BitDir.Rtl ? BitIconName.ChevronLeft : BitIconName.ChevronRight;

    protected override void OnInitialized()
    {
        isDark = Scenario.Theme is WorkbenchThemeMode.Dark;

        cultures = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitChoiceGroupItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];

        tenants = [.. WorkbenchDemoUsers.TenantKeys.Select(key => new BitChoiceGroupItem<string> { Value = key, Text = Localizer[key] })];
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
    }

    private void ChangeCulture(string? culture)
    {
        var uri = new Uri(NavigationManager.Uri);
        var scenario = WorkbenchScenario.Parse(uri.Query) with { Culture = culture };

        NavigationManager.NavigateTo($"{uri.AbsolutePath}{scenario.ToQueryString()}", forceLoad: true);
    }
}
