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
    /// The design systems the picker offers: the library's four, in the library's own order, differing from
    /// <see cref="BitThemeSwitcher.DefaultDesignSystems"/> only in how Fluent's two schemes are spelled.
    /// </summary>
    /// <remarks>
    /// The default list names Fluent's pair <c>light</c> / <c>dark</c>, which is the pair the switcher hands
    /// to BitThemeManager.ToggleDarkLightAsync rather than setting outright, so that a host page stays in
    /// charge of what those two names mean. This host points them at Fluent 2 (see App.razor), which is what
    /// makes Fluent 2 the site's default - so left as they are, toggling the scheme while on Fluent would
    /// switch design system. <c>fluent-light</c> / <c>fluent-dark</c> name the same two palettes outright,
    /// leaving nothing to defer to.
    /// <para>
    /// Fluent stays first, and is therefore still the fallback for a theme none of the items claims: after
    /// the respelling those are exactly the bare <c>light</c> / <c>dark</c> names, which a visitor from
    /// before the Fluent 2 default may still have persisted - and they are core Fluent.
    /// </para>
    /// </remarks>
    private static readonly BitThemeSwitcherItem[] _designSystems =
    [
        new() { Text = "Fluent", Value = BitThemePresets.Fluent, LightTheme = BitThemePresets.FluentLight, DarkTheme = BitThemePresets.FluentDark },
        new() { Text = "Fluent 2", Value = BitExtraThemePresets.Fluent2, LightTheme = BitExtraThemePresets.Fluent2Light, DarkTheme = BitExtraThemePresets.Fluent2Dark },
        new() { Text = "Material", Value = BitExtraThemePresets.Material, LightTheme = BitExtraThemePresets.MaterialLight, DarkTheme = BitExtraThemePresets.MaterialDark },
        new() { Text = "Cupertino", Value = BitExtraThemePresets.Cupertino, LightTheme = BitExtraThemePresets.CupertinoLight, DarkTheme = BitExtraThemePresets.CupertinoDark },
    ];

    /// <summary>
    /// The current path, lower-cased and without the base URI, kept for the active-link check.
    /// </summary>
    private string _currentPath = "/";

    /// <summary>
    /// Which of the five links the bar is currently highlighting, or null while the reader is
    /// somewhere none of them claims. This - rather than the path - is the whole of what the address
    /// decides about this component, so it is what a location change is compared against.
    /// </summary>
    private string? _activeNavHref;

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
        // interactive client comes up with the same selection instead of flashing another one first.
        // Nothing persisted (a first visit, or a visitor following the OS) means the site default,
        // which is Fluent 2 - without it the picker would show the first offered item until hydration.
        // Resolved through the same helper the host page splats onto <html> (see App.razor) rather
        // than compared as a raw string: it trims, lower-cases and validates the token, so a cookie
        // reading " System " or a tampered value lands on whatever the document is painting instead
        // of leaving the picker on a second, different selection. No bit-theme attribute comes back
        // exactly when the OS is being followed or the stored value was unusable.
        var persisted = await PrerenderStateService.GetValue("AppHeader.Theme", () => Task.FromResult(PersistedTheme));
        var rootTheme = BitThemeSsr.BuildRootThemeAttributeMap(persisted);
        _initialTheme = rootTheme.TryGetValue(BitThemeAttributeNames.Theme, out var theme) && theme is string themeName
                        ? themeName
                        : BitExtraThemePresets.Fluent2;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        // Only when the highlighted link actually moved. Nothing else in the bar answers to the
        // address, and rendering it means rendering the search box, the accent swatches and the
        // theme picker - none of which have anything to say about a move from one component page to
        // the next, which is the move that has to be quick. Every one of those hundred navigations
        // leaves "Components" highlighted, and so leaves this bar exactly as it was.
        if (SetCurrentPath() is false) return;

        StateHasChanged();
    }

    /// <summary>Returns whether the highlighted link changed.</summary>
    private bool SetCurrentPath()
    {
        var url = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        var separatorIndex = url.IndexOfAny(['?', '#']);
        if (separatorIndex >= 0) url = url[..separatorIndex];

        _currentPath = url.Length > 1 ? url.TrimEnd('/').ToLowerInvariant() : "/";

        var activeNavHref = Array.Find(_navLinks, link => IsActive(link.Href)).Href;

        if (activeNavHref == _activeNavHref) return false;

        _activeNavHref = activeNavHref;

        return true;
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
