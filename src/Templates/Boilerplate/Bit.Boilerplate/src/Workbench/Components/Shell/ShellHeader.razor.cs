namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The home page's header: the brand, links to the page's sections, the Workbench's language and its own theme.
/// </summary>
public partial class ShellHeader
{
    private List<BitDropdownItem<string>> cultureItems = [];

    [AutoInject] private CultureService cultureService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    protected override void OnInitialized()
    {
        cultureItems = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];
    }

    /// <summary>Saves the culture in the culture cookie and reloads, since a circuit keeps the culture it started with.</summary>
    private async Task SetCulture(string? culture)
    {
        if (string.IsNullOrEmpty(culture) || culture == CultureInfo.CurrentUICulture.Name)
            return;

        await cultureService.ChangeCulture(culture);
    }

    /// <summary>Switches the Workbench's own theme; each view keeps the theme of its scenario.</summary>
    private async Task ToggleTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }
}
