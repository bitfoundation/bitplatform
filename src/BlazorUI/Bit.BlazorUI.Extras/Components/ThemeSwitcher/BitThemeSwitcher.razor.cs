namespace Bit.BlazorUI;

/// <summary>
/// BitThemeSwitcher is the chrome for the two choices a themed app usually puts in its header: which design
/// system it is dressed in (Fluent, Fluent 2, Material, Cupertino - see <see cref="BitExtraThemePresets"/>)
/// and whether that design system is showing its light or its dark scheme. Both halves resolve to one
/// <c>bit-theme</c> name applied through <see cref="BitThemeManager"/>, so the two controls stay two views of
/// a single piece of state: switching the design system keeps the current scheme, and toggling the scheme
/// keeps the current design system.
/// </summary>
/// <remarks>
/// The Fluent 2, Material and Cupertino design systems need their stylesheet bundle linked after the core one
/// to have any effect (see <see cref="BitExtraThemePresets"/>); offering an item whose bundle the host page
/// does not link leaves the app on the Fluent defaults.
/// </remarks>
public partial class BitThemeSwitcher : BitComponentBase
{
    /// <summary>
    /// The design systems offered when <see cref="DesignSystems"/> is not set: the four that ship with the
    /// library. Fluent comes first, and is therefore also the fallback for an applied theme that none of the
    /// items claim (a custom preset, or the <c>system</c> pseudo-preset) - it is the design system the core
    /// stylesheet carries, so it is what such a theme is actually painted with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fluent's pair is spelled <c>light</c> / <c>dark</c> rather than <c>fluent-light</c> / <c>fluent-dark</c>
    /// (which name the same palettes) because those two are the theme names
    /// <see cref="BitThemeManager.ToggleDarkLightAsync"/> flips between by default - see the toggle handler.
    /// </para>
    /// <para>
    /// Fluent 2 follows Fluent rather than sitting at the end: it is the same design language one generation
    /// on, so the two read as a pair. <c>fluent</c> is a prefix of <c>fluent2</c> as plain text, but
    /// <see cref="FindDesignSystem"/> matches a family on <c>"{Value}-"</c>, and <c>fluent2-dark</c> does not
    /// start with <c>fluent-</c> - so neither of the two ever claims the other's theme names.
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyList<BitThemeSwitcherItem> DefaultDesignSystems =
    [
        new() { Text = "Fluent", Value = "fluent", LightTheme = BitThemePresets.Light, DarkTheme = BitThemePresets.Dark },
        new() { Text = "Fluent 2", Value = BitExtraThemePresets.Fluent2, LightTheme = BitExtraThemePresets.Fluent2Light, DarkTheme = BitExtraThemePresets.Fluent2Dark },
        new() { Text = "Material", Value = BitExtraThemePresets.Material, LightTheme = BitExtraThemePresets.MaterialLight, DarkTheme = BitExtraThemePresets.MaterialDark },
        new() { Text = "Cupertino", Value = BitExtraThemePresets.Cupertino, LightTheme = BitExtraThemePresets.CupertinoLight, DarkTheme = BitExtraThemePresets.CupertinoDark },
    ];



    [Inject] private BitThemeManager _themeManager { get; set; } = default!;

    [Inject] private BitThemeNotifications _themeNotifications { get; set; } = default!;



    /// <summary>The design systems this instance offers, resolved from <see cref="DesignSystems"/>.</summary>
    private BitThemeSwitcherItem[] _designSystems = [];

    /// <summary>The <see cref="BitDropdown{TItem, TValue}"/> projection of the offered design systems.</summary>
    private List<BitDropdownItem<string>> _items = [];

    /// <summary>The <see cref="BitThemeSwitcherItem.Value"/> of the selected design system.</summary>
    private string? _designSystem;



    /// <summary>
    /// Custom CSS classes for different parts of the switcher.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitThemeSwitcherClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon of the button that switches to the dark scheme - the one shown while the light scheme is
    /// active. A Fluent UI (Fabric MDL2) icon name, rendered through the <c>Bit.BlazorUI.Icons</c> stylesheet.
    /// </summary>
    [Parameter] public string DarkSchemeIconName { get; set; } = "ClearNight";

    /// <summary>
    /// The title and aria-label of the button that switches to the dark scheme.
    /// </summary>
    [Parameter] public string? DarkSchemeTitle { get; set; } = "Turn off light";

    /// <summary>
    /// The design systems offered by the picker. Defaults to <see cref="DefaultDesignSystems"/>. The first
    /// item is also the fallback the picker shows for an applied theme that no item claims.
    /// </summary>
    [Parameter] public IEnumerable<BitThemeSwitcherItem>? DesignSystems { get; set; }

    /// <summary>
    /// The title and aria-label of the design system picker.
    /// </summary>
    [Parameter] public string? DesignSystemTitle { get; set; } = "Design system";

    /// <summary>
    /// The theme to reflect until the applied one can be read back, which takes JS interop and therefore a
    /// first interactive render. Hand it the theme the app persisted (the
    /// <see cref="BitThemeCookie.PreferenceCookieName"/> cookie, which the client mirrors the choice into
    /// when the host page opts in with <see cref="BitThemeAttributeNames.ThemePersistCookie"/>) so
    /// prerendered markup shows the visitor's own design system instead of showing the first item until
    /// hydration.
    /// </summary>
    [Parameter] public string? InitialTheme { get; set; }

    /// <summary>
    /// The icon of the button that switches to the light scheme - the one shown while the dark scheme is
    /// active. A Fluent UI (Fabric MDL2) icon name, rendered through the <c>Bit.BlazorUI.Icons</c> stylesheet.
    /// </summary>
    [Parameter] public string LightSchemeIconName { get; set; } = "Sunny";

    /// <summary>
    /// The title and aria-label of the button that switches to the light scheme.
    /// </summary>
    [Parameter] public string? LightSchemeTitle { get; set; } = "Turn on light";

    /// <summary>
    /// Hides the light/dark toggle, leaving only the design system picker.
    /// </summary>
    [Parameter] public bool NoColorScheme { get; set; }

    /// <summary>
    /// Hides the design system picker, leaving only the light/dark toggle.
    /// </summary>
    [Parameter] public bool NoDesignSystem { get; set; }

    /// <summary>
    /// The callback that is called when the theme changes, receiving the applied theme name.
    /// </summary>
    [Parameter] public EventCallback<string> OnChange { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the switcher.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitThemeSwitcherClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-ths";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        _designSystems = [.. (DesignSystems ?? DefaultDesignSystems).Where(i => i.Value.HasValue())];
        _items = [.. _designSystems.Select(i => new BitDropdownItem<string>
        {
            Text = i.Text ?? i.Value,
            Value = i.Value,
            AriaLabel = i.AriaLabel ?? i.Text,
            IsEnabled = i.IsEnabled
        })];

        // Only until the first interactive render, which reads the applied theme back and takes over.
        _designSystem ??= ResolveDesignSystem(InitialTheme);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && NoDesignSystem is false)
        {
            // Subscribed before the read below rather than after it: a theme applied in between - by another
            // switcher on the page, or by the app itself - would otherwise fall in the gap and leave this
            // picker showing a design system the page is no longer wearing.
            _themeNotifications.ThemeChanged += HandleOnThemeChanged;

            // The theme script restores the persisted theme on its own, so the page is already painted in
            // the right design system by the time this runs; it is the picker that would still be showing
            // whatever InitialTheme resolved to. Read the applied theme back - JS interop, hence after the
            // first render rather than in OnInitialized - and select the design system it belongs to.
            var current = await _themeManager.GetCurrentThemeAsync();

            // A null read means the runtime could not be asked (a disconnected circuit, JS interop otherwise
            // unavailable), not that no theme is applied - so it must not be resolved like a theme name, which
            // would answer "the first design system" and throw away what InitialTheme knew.
            if (current.HasValue())
            {
                var applied = ResolveDesignSystem(current);

                if (applied != _designSystem)
                {
                    _designSystem = applied;
                    StateHasChanged();
                }
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    /// <summary>The theme name this design system's light scheme is spelled with.</summary>
    private static string GetLightTheme(BitThemeSwitcherItem item) => item.LightTheme ?? $"{item.Value}-light";

    /// <summary>The theme name this design system's dark scheme is spelled with.</summary>
    private static string GetDarkTheme(BitThemeSwitcherItem item) => item.DarkTheme ?? $"{item.Value}-dark";

    /// <summary>
    /// Whether a theme name is a dark one. Matched on the suffix rather than against a list of known names,
    /// for the same reason the stylesheet's <c>[bit-theme$="dark"]</c> selector is: the two decide the same
    /// thing - which scheme is showing - so a custom preset following the <c>-dark</c> convention has to read
    /// the same way to both, or the toggle and the icon it toggles would disagree.
    /// </summary>
    private static bool IsDarkTheme(string? theme) => theme?.EndsWith("dark", StringComparison.OrdinalIgnoreCase) is true;

    /// <summary>
    /// The design system a theme name belongs to, or null when none of the offered ones claims it. A name
    /// counts as claimed when it is the design system's own token, either of its two scheme names, or any
    /// other name prefixed with the token (so a preset family's further variants come along).
    /// </summary>
    private BitThemeSwitcherItem? FindDesignSystem(string? theme)
    {
        if (theme.HasValue() is false) return null;

        return _designSystems.FirstOrDefault(item =>
            string.Equals(theme, item.Value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(theme, GetLightTheme(item), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(theme, GetDarkTheme(item), StringComparison.OrdinalIgnoreCase) ||
            theme!.StartsWith($"{item.Value}-", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The <see cref="BitThemeSwitcherItem.Value"/> the picker shows for a theme name, falling back to the
    /// first offered design system for a theme none of them claims.
    /// </summary>
    private string? ResolveDesignSystem(string? theme) => (FindDesignSystem(theme) ?? _designSystems.FirstOrDefault())?.Value;

    /// <summary>
    /// Follows the applied theme for the rest of the component's life, the way the first interactive render
    /// picks it up once: the theme is a document-wide piece of state that anything can change - a sibling
    /// switcher, the app's own code, or the OS while following the system theme - and a picker that only
    /// read it at startup would go stale on the first such change.
    /// </summary>
    private void HandleOnThemeChanged(object? sender, BitThemeChangedEventArgs e)
    {
        // A null new theme means "unknown" rather than a theme name (see BitThemeChangedEventArgs), so it is
        // left alone for the same reason the read at startup is: resolving it would answer "the first design
        // system" and overwrite what the picker already knows.
        if (e.NewTheme.HasValue() is false) return;

        var applied = ResolveDesignSystem(e.NewTheme);

        // Raised from the JS interop callback thread, hence the hop back onto the renderer's before touching
        // component state.
        _ = InvokeAsync(() =>
        {
            if (applied == _designSystem) return;

            _designSystem = applied;
            StateHasChanged();
        });
    }

    private string GetDesignSystemClass()
    {
        return string.Join(' ', new[] { "bit-ths-dds", Classes?.DesignSystem }.Where(c => c.HasValue()));
    }

    private string GetColorSchemeClass(string schemeClass, string? schemeCustomClass)
    {
        return string.Join(' ', new[] { "bit-ths-csb", schemeClass, Classes?.ColorSchemeButton, schemeCustomClass }.Where(c => c.HasValue()));
    }

    private string GetColorSchemeStyle(string? schemeCustomStyle)
    {
        return string.Join(';', new[] { Styles?.ColorSchemeButton, schemeCustomStyle }.Where(s => s.HasValue()));
    }

    private async Task HandleOnSelectDesignSystem(BitDropdownItem<string> selected)
    {
        if (IsEnabled is false) return;

        var item = _designSystems.FirstOrDefault(i => i.Value == selected.Value);
        if (item is null) return;

        // The scheme is the half of the theme this control does not decide, so it is carried over from
        // whatever is applied rather than reset to light.
        var target = IsDarkTheme(await _themeManager.GetCurrentThemeAsync()) ? GetDarkTheme(item) : GetLightTheme(item);

        await _themeManager.SetThemeAsync(target);

        _designSystem = item.Value;

        await OnChange.InvokeAsync(target);
    }

    private async Task HandleOnToggleColorScheme()
    {
        if (IsEnabled is false) return;

        var current = await _themeManager.GetCurrentThemeAsync();

        // The picker's own selection is the fallback rather than the first design system: it is what the
        // visitor is looking at, and it is all there is to go on when the applied theme cannot be read back.
        var item = FindDesignSystem(current)
                ?? _designSystems.FirstOrDefault(i => i.Value == _designSystem)
                ?? _designSystems.FirstOrDefault();
        if (item is null) return;

        var light = GetLightTheme(item);
        var dark = GetDarkTheme(item);
        var target = IsDarkTheme(current) ? light : dark;

        string? applied;
        if (light == BitThemePresets.Light && dark == BitThemePresets.Dark)
        {
            // The core light/dark pair is the one the JS toggle flips between by default, and a host page can
            // retarget it with the bit-theme-light / bit-theme-dark attributes. Going through the manager
            // therefore leaves such a host in charge of what "light" and "dark" mean; every other pair names
            // both of its themes outright, so there is nothing to defer to and they are set directly.
            applied = await _themeManager.ToggleDarkLightAsync();
        }
        else
        {
            await _themeManager.SetThemeAsync(target);
            applied = target;
        }

        // Nothing to re-render for: which of the two buttons is visible follows the document's own bit-theme
        // attribute in CSS, and the design system is unchanged by definition.
        await OnChange.InvokeAsync(applied ?? target);
    }



    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed is false && disposing)
        {
            // BitThemeNotifications is scoped, so a leaked handler would keep this component rooted for the
            // lifetime of the circuit and re-render it after teardown.
            _themeNotifications.ThemeChanged -= HandleOnThemeChanged;
        }

        return base.DisposeAsync(disposing);
    }
}
