namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The home page's header: the brand, links to the page's sections and to the agents' progress with what waits for the
/// user, the Workbench's language and its own theme.
/// </summary>
public partial class ShellHeader
{
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private WorkbenchReviewSession reviewSession = default!;

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        await base.DisposeAsync();
    }

    /// <summary>Switches the Workbench's own theme; each view keeps the theme of its scenario.</summary>
    private async Task ToggleTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }
}
