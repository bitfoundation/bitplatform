namespace Boilerplate.Workbench.Components.Pages.LandingPages.Minimal;

/// <summary>
/// One of the landing page's samples in a view. It isn't a page of the app, so no app shell is drawn around it, and it sets
/// the theme's tokens for itself. Theme and language go through the view's settings, as in the app's own menu.
/// </summary>
public partial class MinimalLandingPage
{
    private static readonly (string Title, string Body)[] features =
    [
        (nameof(WorkbenchStrings.NotchFeature1Title), nameof(WorkbenchStrings.NotchFeature1Body)),
        (nameof(WorkbenchStrings.NotchFeature2Title), nameof(WorkbenchStrings.NotchFeature2Body)),
        (nameof(WorkbenchStrings.NotchFeature3Title), nameof(WorkbenchStrings.NotchFeature3Body))
    ];

    private static readonly (string Icon, string Title, string Body, string? Tag, bool IsNever)[] route =
    [
        (BitIconName.CellPhone, nameof(WorkbenchStrings.NotchRoutePhoneTitle), nameof(WorkbenchStrings.NotchRoutePhoneBody), null, false),
        (BitIconName.Encryption, nameof(WorkbenchStrings.NotchRouteBackupTitle), nameof(WorkbenchStrings.NotchRouteBackupBody), nameof(WorkbenchStrings.NotchRouteOptional), false),
        (BitIconName.Server, nameof(WorkbenchStrings.NotchRouteServerTitle), nameof(WorkbenchStrings.NotchRouteServerBody), null, false),
        (BitIconName.Blocked2, nameof(WorkbenchStrings.NotchRouteNeverTitle), nameof(WorkbenchStrings.NotchRouteNeverBody), nameof(WorkbenchStrings.NotchRouteNever), true)
    ];

    [AutoInject] private MinimalLandingStore store = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    private bool isDark;

    private string ThemeToggleLabel => isDark
        ? AppLocalizer[nameof(AppStrings.SwitchToLightThemeTitle)]
        : AppLocalizer[nameof(AppStrings.SwitchToDarkThemeTitle)];

    /// <summary>The languages the landing page ships, other than the one on the screen.</summary>
    private static IEnumerable<(string Name, string Culture)> OtherCultures =>
        CultureInfoManager.SupportedCultures
            .Where(sc => sc.Culture.Name is "en-US" or "fa-IR" && sc.Culture.Name != CultureInfo.CurrentUICulture.Name)
            .Select(sc => (sc.DisplayName, sc.Culture.Name));

    protected override void OnInitialized()
    {
        Subscribe(store);

        isDark = Scenario.Theme is WorkbenchThemeMode.Dark;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || Scenario.Theme is not WorkbenchThemeMode.System)
            return;

        isDark = (await bitThemeManager.GetCurrentThemeAsync())?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;
        StateHasChanged();
    }

    /// <summary>A link to a section of this page in this view; the host page's base address would lead a plain hash to the Workbench's home page.</summary>
    private string SectionUrl(string id) => $"{NavigationManager.Uri.Split('#')[0]}#{id}";

    private async Task ToggleTheme()
    {
        var theme = await bitThemeManager.ToggleDarkLightAsync();
        isDark = theme?.Contains("dark", StringComparison.OrdinalIgnoreCase) is true;

        await ReportScenario(Scenario with { Theme = isDark ? WorkbenchThemeMode.Dark : WorkbenchThemeMode.Light });
    }

    private Task ChangeCulture(string culture) => ReportScenario(Scenario with { Culture = culture == CultureInfoManager.DefaultCulture.Name ? null : culture }, isApplied: false);
}
