namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class AppHeader
{
    /// <summary>
    /// The theme BitThemeSwitcher shows until it can read the applied one back, which takes JS interop and
    /// therefore a first interactive render.
    /// </summary>
    private string? _initialTheme;


    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;


    [Parameter] public bool IsHomePage { get; set; }
    [Parameter] public EventCallback OnToggleNavPanel { get; set; }

    /// <summary>
    /// The persisted theme preference (the bit-theme-preference cookie), cascaded by the server host
    /// while prerendering; null everywhere else.
    /// </summary>
    [CascadingParameter(Name = "PersistedTheme")] public string? PersistedTheme { get; set; }


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // Prerendering has no JS runtime to ask, so the server reads the persisted theme from its
        // cookie and cascades it here; the value is then persisted into the prerendered state so the
        // interactive client comes up with the same selection instead of flashing "Fluent" first.
        _initialTheme = await PrerenderStateService.GetValue("AppHeader.Theme", () => Task.FromResult(PersistedTheme));
    }

    private async Task ToggleNavMenu()
    {
        await OnToggleNavPanel.InvokeAsync();
    }

    /// <summary>
    /// Keeps the native shell in step with the theme the switcher applied: on MAUI the status bar and the
    /// window chrome are painted by the platform, not by the page, so they only follow the web content when
    /// they are told to. Nothing to do on the web, where the coordinator is a no-op.
    /// </summary>
    private async Task OnThemeChanged(string theme)
    {
        await _bitDeviceCoordinator.ApplyTheme(theme.EndsWith("dark", StringComparison.OrdinalIgnoreCase));
    }
}
