using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class AppHeader
{
    /// <summary>
    /// The theme BitThemeSwitcher shows until it can read the applied one back, which takes JS interop and
    /// therefore a first interactive render.
    /// </summary>
    private string? _initialTheme;

    /// <summary>
    /// The current path, lower-cased and without the base URI, kept for the active-link check.
    /// </summary>
    private string _currentPath = "/";

    /// <summary>
    /// The site's five destinations, in the order a visitor meets them: what it is, what is in it,
    /// how to make it yours, and the icon set. Anything deeper is the nav panel's job.
    /// </summary>
    private static readonly (string Text, string Href)[] _navLinks =
    [
        ("Home", "/"),
        ("Docs", "/overview"),
        ("Components", "/components"),
        ("Theming", "/theming"),
        ("Icons", "/iconography"),
    ];


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

        SetCurrentPath();
        NavigationManager.LocationChanged += OnLocationChanged;

        // Prerendering has no JS runtime to ask, so the server reads the persisted theme from its
        // cookie and cascades it here; the value is then persisted into the prerendered state so the
        // interactive client comes up with the same selection instead of flashing "Fluent" first.
        _initialTheme = await PrerenderStateService.GetValue("AppHeader.Theme", () => Task.FromResult(PersistedTheme));
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentPath();
        StateHasChanged();
    }

    private void SetCurrentPath()
    {
        var url = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        var separatorIndex = url.IndexOfAny(['?', '#']);
        if (separatorIndex >= 0) url = url[..separatorIndex];

        _currentPath = url.Length > 1 ? url.TrimEnd('/').ToLowerInvariant() : "/";
    }

    /// <summary>
    /// Which of the five links is the section the reader is currently in. "Home" only matches the
    /// home page itself; every component page counts as "Components", so the bar keeps pointing at
    /// the section rather than going blank as soon as the reader opens a component.
    /// </summary>
    private bool IsActive(string href)
    {
        if (href == "/") return _currentPath == "/";

        if (href == "/components") return _currentPath.StartsWith("/components", StringComparison.Ordinal);

        return _currentPath == href;
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


    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        return base.DisposeAsync(disposing);
    }
}
