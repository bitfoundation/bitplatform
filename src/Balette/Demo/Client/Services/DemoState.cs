using Microsoft.JSInterop;

namespace Bit.Balette.Demo.Client.Services;

/// <summary>
/// The application state the demo's commands act on - a theme, three preferences, a project with labels and an
/// activity log - and the way the layout's palette is presented. It is scoped, so the layout, every page and every
/// command handler share one instance: a value changed from the palette shows on the page, and one changed on the
/// page shows in the palette the next time it renders.
/// </summary>
public sealed class DemoState
{
    // The same key the inline script in App.razor reads before the first paint.
    private const string ThemeStorageKey = "bit-balette-demo.theme";

    private readonly IJSRuntime _js;
    private readonly List<string> _log = [];

    public DemoState(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Raised after any value changed. The layout re-registers its commands on it, so toggles show their new state.</summary>
    public event Action? Changed;

    /// <summary><c>auto</c>, <c>light</c> or <c>dark</c>, written to <c>&lt;html data-theme&gt;</c> - which the site and the palette both follow.</summary>
    public string Theme { get; private set; } = "auto";

    public bool WordWrap { get; private set; } = true;

    public bool Notifications { get; private set; }

    public bool ShowMinimap { get; private set; } = true;

    public string ProjectName { get; private set; } = "Acme Dashboard";

    public IReadOnlyList<string> Labels { get; private set; } = ["bug"];

    /// <summary>Newest first, capped at 30 entries.</summary>
    public IReadOnlyList<string> Log => _log;

    // ---- Presentation of the layout's palette ------------------------------------------------------------------

    /// <summary>The palette's own Theme parameter. Null follows the page; a value wins over it.</summary>
    public string? PaletteTheme { get; private set; }

    /// <summary>An accent colour poured into the palette's custom properties, or null for the stylesheet's.</summary>
    public string? Accent { get; private set; }

    /// <summary>The dialog width in pixels, or null for the stylesheet's (640, or 920 with the detail pane).</summary>
    public int? Width { get; private set; }

    /// <summary>The corner radius in pixels, or null for the stylesheet's.</summary>
    public int? Radius { get; private set; }

    public bool ShowDetail { get; private set; } = true;

    public bool ShowFooter { get; private set; } = true;

    public bool HighlightMatches { get; private set; } = true;

    public bool GroupWhenSearching { get; private set; } = true;

    public bool Virtualize { get; private set; }

    /// <summary>
    /// The palette's Style parameter: inline custom properties on the dialog, which win over every rule in the
    /// stylesheet. The soft accent and the highlight are derived from the accent so one choice recolours all three.
    /// </summary>
    public string? PaletteStyle
    {
        get
        {
            var parts = new List<string>(5);
            if (Accent is not null)
            {
                parts.Add($"--bit-balette-accent:{Accent}");
                parts.Add($"--bit-balette-accent-soft:color-mix(in srgb, {Accent} 14%, transparent)");
                parts.Add($"--bit-balette-mark-bg:color-mix(in srgb, {Accent} 26%, transparent)");
            }

            if (Width is { } width)
            {
                parts.Add($"--bit-balette-width:{width}px");
            }

            if (Radius is { } radius)
            {
                parts.Add($"--bit-balette-radius:{radius}px");
            }

            return parts.Count == 0 ? null : string.Join(';', parts);
        }
    }

    // ---- Theme ---------------------------------------------------------------------------------------------------

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme;
        try
        {
            await _js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", theme);
            await _js.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, theme);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
            // Storage can be disabled; the theme still applies for this visit.
        }

        Record($"Theme set to {theme}");
    }

    /// <summary>
    /// Picks up the theme the inline script in App.razor already applied from storage, so a reload does not show
    /// "auto" in the palette while the page is dark. Called once the layout runs interactively.
    /// </summary>
    public async Task RestoreThemeAsync()
    {
        try
        {
            var applied = await _js.InvokeAsync<string?>("document.documentElement.getAttribute", "data-theme");
            if (applied is "light" or "dark" && applied != Theme)
            {
                Theme = applied;
                Changed?.Invoke();
            }
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or JSDisconnectedException)
        {
        }
    }

    // ---- Preferences ---------------------------------------------------------------------------------------------

    public void ToggleWordWrap()
    {
        WordWrap = !WordWrap;
        Record($"Word wrap {(WordWrap ? "on" : "off")}");
    }

    public void ToggleNotifications()
    {
        Notifications = !Notifications;
        Record($"Notifications {(Notifications ? "on" : "off")}");
    }

    public void ToggleMinimap()
    {
        ShowMinimap = !ShowMinimap;
        Record($"Minimap {(ShowMinimap ? "shown" : "hidden")}");
    }

    public async Task ResetPreferencesAsync()
    {
        WordWrap = true;
        Notifications = false;
        ShowMinimap = true;
        await SetThemeAsync("auto");
    }

    public void Rename(string name)
    {
        ProjectName = name;
        Record($"Project renamed to “{name}”");
    }

    public void SetLabels(IEnumerable<string> labels)
    {
        Labels = [.. labels];
        Record(Labels.Count == 0 ? "Labels cleared" : $"Labels: {string.Join(", ", Labels)}");
    }

    // ---- Palette presentation ------------------------------------------------------------------------------------

    public void SetPaletteTheme(string? theme) => Set(() => PaletteTheme = theme);

    public void SetAccent(string? accent) => Set(() => Accent = accent);

    public void SetWidth(int? width) => Set(() => Width = width);

    public void SetRadius(int? radius) => Set(() => Radius = radius);

    public void SetShowDetail(bool value) => Set(() => ShowDetail = value);

    public void SetShowFooter(bool value) => Set(() => ShowFooter = value);

    public void SetHighlightMatches(bool value) => Set(() => HighlightMatches = value);

    public void SetGroupWhenSearching(bool value) => Set(() => GroupWhenSearching = value);

    public void SetVirtualize(bool value) => Set(() => Virtualize = value);

    public void ResetPresentation() => Set(() =>
    {
        PaletteTheme = null;
        Accent = null;
        Width = null;
        Radius = null;
        ShowDetail = true;
        ShowFooter = true;
        HighlightMatches = true;
        GroupWhenSearching = true;
        Virtualize = false;
    });

    // ---- Activity log --------------------------------------------------------------------------------------------

    public void Record(string message)
    {
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        if (_log.Count > 30)
        {
            _log.RemoveAt(_log.Count - 1);
        }

        Changed?.Invoke();
    }

    private void Set(Action change)
    {
        change();
        Changed?.Invoke();
    }
}
