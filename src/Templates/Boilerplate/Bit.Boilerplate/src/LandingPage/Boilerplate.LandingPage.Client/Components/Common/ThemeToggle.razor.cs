namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class ThemeToggle
{
    [AutoInject] private BitThemeManager themeManager = default!;

    [Parameter] public string? Class { get; set; }

    private bool isDark;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isDark = (await themeManager.GetCurrentThemeAsync())?.EndsWith("dark", StringComparison.OrdinalIgnoreCase) is true;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Toggle()
    {
        isDark = (await themeManager.ToggleDarkLightAsync())?.EndsWith("dark", StringComparison.OrdinalIgnoreCase) is true;
    }
}
