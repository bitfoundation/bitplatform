namespace Bit.BlazorUI;

/// <summary>
/// BitAccentColorSwitcher renders a row of accent color swatches that re-theme the whole app live:
/// one picked brand color is fed to BitThemeFactory as the seed of a complete palette, applied
/// through BitThemeManager, and optionally persisted to localStorage and/or a cookie (see
/// Persistence) so it survives a refresh - with
/// selectable first-paint strategies that keep the accent correct even when the served HTML comes
/// from a cache, before any Blazor runtime is up (see BitAccentColorFirstPaintStrategy and
/// BitAccentColorSsr).
/// </summary>
public partial class BitAccentColorSwitcher : BitComponentBase
{
    /// <summary>
    /// The accent colors offered when the <see cref="Accents"/> parameter is not set: the six
    /// <see cref="BitAccentColorPresets"/> hues, starting with the packaged palette's own primary.
    /// </summary>
    public static readonly IReadOnlyList<BitAccentColorItem> DefaultAccents =
    [
        new() { Name = "Blue", Color = BitAccentColorPresets.Blue },
        new() { Name = "Purple", Color = BitAccentColorPresets.Purple },
        new() { Name = "Green", Color = BitAccentColorPresets.Green },
        new() { Name = "Orange", Color = BitAccentColorPresets.Orange },
        new() { Name = "Teal", Color = BitAccentColorPresets.Teal },
        new() { Name = "Rose", Color = BitAccentColorPresets.Rose },
    ];



    [Inject] private BitAccentColorService _accentColorService { get; set; } = default!;



    /// <summary>
    /// The accent colors to offer as swatches. When null, the DefaultAccents (the six
    /// BitAccentColorPresets hues) are offered.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAccentColorItem>? Accents { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the switcher.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAccentColorSwitcherClassStyles? Classes { get; set; }

    /// <summary>
    /// The first-paint strategy to maintain when applying an accent: None (the default) applies the
    /// accent after hydration only, StaticCss keys a prebuilt all-accents stylesheet on the
    /// bit-accent root attribute, StoredCss keeps a snapshot of the generated palette CSS in
    /// localStorage. See BitAccentColorFirstPaintStrategy and BitAccentColorSsr for the
    /// server/head-script halves each strategy needs. The strategy is app-wide state on the shared
    /// BitAccentColorService - the first initialized instance (or an explicit
    /// BitAccentColorService.InitializeAsync call) fixes it, so keep it identical across instances.
    /// With a CSS strategy the built-in active ring additionally keys on the bit-accent root
    /// attribute the inline head script sets pre-paint, so prerendered and cached markup rings the
    /// visitor's swatch immediately instead of ringing the default until hydration restores the accent.
    /// </summary>
    [Parameter] public BitAccentColorFirstPaintStrategy FirstPaintStrategy { get; set; }

    /// <summary>
    /// The callback that is called when the accent color changes, receiving the applied accent color.
    /// </summary>
    [Parameter] public EventCallback<string> OnChange { get; set; }

    /// <summary>
    /// The stores the picked accent is persisted to: LocalStorage, Cookie, or both (All); None (the
    /// default) keeps the accent for the current session only. The cookie half is what lets the
    /// server read the preference while prerendering (SSR) - see BitAccentColorSsr - so enable it
    /// when the server takes part in painting or seeding the accent. Like FirstPaintStrategy, this
    /// is app-wide state on the shared BitAccentColorService fixed by the first initialized
    /// instance, so keep it identical across instances.
    /// </summary>
    [Parameter] public BitAccentColorPersistence Persistence { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the switcher.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAccentColorSwitcherClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-acs";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        // Subscribed this early - rather than after the first render - so the restore the first
        // interactive render kicks off cannot land in the gap between rendering the swatches and
        // listening for the accent they mark.
        _accentColorService.AccentChanged += OnAccentChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _accentColorService.InitializeAsync(Accents, FirstPaintStrategy, Persistence);
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private string GetSwatchClass(bool isActive, bool cssMarked)
    {
        return string.Join(' ', new[]
        {
            "bit-acs-swt",
            // With a CSS first-paint strategy the built-in ring comes from GetPrePaintActiveMarkerCss
            // (keyed on the bit-accent root attribute) instead of this class, so prerendered markup -
            // where the C# state still holds the default - cannot ring the wrong swatch.
            isActive && cssMarked is false ? "bit-acs-act" : null,
            Classes?.Swatch,
            isActive ? Classes?.ActiveSwatch : null
        }.Where(c => c.HasValue()));
    }

    /// <summary>
    /// One rule ringing the swatch whose token the <c>bit-accent</c> root attribute carries (plus
    /// the packaged primary's swatch when no attribute is set, i.e. "no override"). Scoped to this
    /// instance through its id, so instances with different accent lists cannot cross-match. The
    /// declarations mirror <c>.bit-acs-act</c> in <c>BitAccentColorSwitcher.scss</c>.
    /// </summary>
    private string GetPrePaintActiveMarkerCss()
    {
        var selectors = new List<string>();
        var neutralToken = BitAccentColorSsr.NormalizeToken(BitAccentColorPresets.Blue);

        foreach (var item in Accents ?? DefaultAccents)
        {
            var token = BitAccentColorSsr.NormalizeToken(item.Color);
            if (token is null) continue;

            selectors.Add($":root[{BitAccentColorNames.Attribute}=\"{token}\"] [id=\"{_Id}\"] [bit-accent-swatch=\"{token}\"]");

            if (token == neutralToken)
            {
                selectors.Add($":root:not([{BitAccentColorNames.Attribute}]) [id=\"{_Id}\"] [bit-accent-swatch=\"{token}\"]");
            }
        }

        if (selectors.Count is 0) return string.Empty;

        return $"{string.Join(',', selectors)}{{outline:0.125rem solid var(--bit-acs-clr);outline-offset:0.125rem;}}";
    }

    private string GetSwatchStyle(BitAccentColorItem item, bool isActive)
    {
        return string.Join(';', new[]
        {
            $"--bit-acs-clr:{item.Color}",
            Styles?.Swatch,
            isActive ? Styles?.ActiveSwatch : null
        }.Where(s => s.HasValue()));
    }

    private static string GetSwatchAriaLabel(BitAccentColorItem item)
    {
        if (item.AriaLabel.HasValue()) return item.AriaLabel;

        return item.Name.HasValue() ? $"Apply the {item.Name} accent color" : "Apply this accent color";
    }

    private async Task HandleOnClickAsync(BitAccentColorItem item)
    {
        if (IsEnabled is false) return;

        // The service applies per the app-wide configuration the first InitializeAsync call fixed;
        // this instance's parameters are not forwarded, so a differently-parameterized switcher
        // cannot tear down the stores and attribute its siblings maintain.
        await _accentColorService.ApplyAsync(item.Color);

        await OnChange.InvokeAsync(item.Color);
    }

    private void OnAccentChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }



    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed is false && disposing)
        {
            _accentColorService.AccentChanged -= OnAccentChanged;
        }

        return base.DisposeAsync(disposing);
    }
}
