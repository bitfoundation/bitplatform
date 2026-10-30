namespace Boilerplate.Workbench.Components.Shell;

public partial class ShellHeader
{
    private List<BitDropdownItem<string>> cultureItems = [];

    [AutoInject] private BitThemeManager bitThemeManager = default!;

    protected override void OnInitialized()
    {
        cultureItems = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];
    }

    private async Task SetCulture(string? culture)
    {
        if (string.IsNullOrEmpty(culture) || culture == CultureInfo.CurrentUICulture.Name)
            return;

        await JSRuntime.InvokeVoidAsync("workbench.setCulture", culture);
    }

    private async Task ToggleTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }
}
