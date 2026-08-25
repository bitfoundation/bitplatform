using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

/// <summary>
/// BitAccentColorSwitcher renders a row of accent color swatches that re-theme the whole app live:
/// one picked brand color is fed to BitThemeFactory as the seed of a complete palette, applied
/// through BitThemeManager, and optionally persisted to localStorage and/or a cookie (see
/// BitAccentColorConfig.Persistence) so it survives a refresh - with
/// selectable first-paint strategies that keep the accent correct even when the served HTML comes
/// from a cache, before any Blazor runtime is up (see BitAccentColorFirstPaintStrategy and
/// BitAccentColorSsr).
/// </summary>
public partial class BitAccentColorSwitcher : BitComponentBase
{
    /// <summary>
    /// The accent colors offered when <see cref="BitAccentColorConfig.Accents"/> is not set (on the
    /// <see cref="Config"/> parameter nor on the configuration registered in DI): the six
    /// <see cref="BitAccentColorPresets"/> hues, starting with the packaged palette's own primary.
    /// That first item is named "Default" rather than after a hue: its token is the blue the service
    /// treats as "no override", but the swatch paints the primary of whichever packaged preset is
    /// active (iOS blue under Cupertino - see the --bit-acs-ntr custom property in
    /// BitAccentColorSwitcher.scss), so naming it "Blue" would be wrong there.
    /// </summary>
    public static readonly IReadOnlyList<BitAccentColorItem> DefaultAccents =
    [
        new() { Name = "Default", Color = BitAccentColorPresets.Blue },
        new() { Name = "Purple", Color = BitAccentColorPresets.Purple },
        new() { Name = "Green", Color = BitAccentColorPresets.Green },
        new() { Name = "Orange", Color = BitAccentColorPresets.Orange },
        new() { Name = "Teal", Color = BitAccentColorPresets.Teal },
        new() { Name = "Rose", Color = BitAccentColorPresets.Rose },
    ];



    [Inject] private BitAccentColorService _accentColorService { get; set; } = default!;

    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The configuration this instance renders and initializes by: the Config parameter when one is
    /// handed in, otherwise the app-wide instance registered in DI (the accentColor option of
    /// AddBitBlazorUIExtrasServices), otherwise null (all defaults).
    /// </summary>
    private BitAccentColorConfig? _config;

    /// <summary>
    /// Set once this instance is interactive and the accent has been restored, which is when the C#
    /// accent state becomes the authority on which swatch to mark. Until then a CSS strategy leaves
    /// the marking to <see cref="BitAccentColorSsr.BuildSwatchMarkerCss"/>, because prerendered
    /// markup can be served from a cache to a visitor whose accent the server never saw.
    /// </summary>
    private bool _hydrated;



    /// <summary>
    /// Custom CSS classes for different parts of the switcher.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAccentColorSwitcherClassStyles? Classes { get; set; }

    /// <summary>
    /// The app-wide accent configuration: the accents offered as swatches, the stores the picked
    /// one is persisted to, and the first-paint strategy to maintain when applying it. When null,
    /// the BitAccentColorConfig registered in DI (the accentColor option of
    /// AddBitBlazorUIExtrasServices) is used; with neither, the DefaultAccents are offered, nothing
    /// is persisted and no first-paint machinery runs. The configuration is app-wide state on the
    /// shared BitAccentColorService - the first initialized instance (or an explicit
    /// BitAccentColorService.InitializeAsync call) fixes it - so state it once: register it in DI,
    /// or define one shared BitAccentColorConfig instance and hand the same one to every switcher
    /// and to the host page's BitAccentColorHead. With a CSS strategy the built-in active ring comes,
    /// until this instance is interactive, from the marker CSS that BitAccentColorHead emits for its
    /// own accents - keyed on the bit-accent root attribute the inline head script sets pre-paint, so
    /// prerendered and cached markup rings the visitor's swatch immediately instead of ringing the
    /// default until hydration restores the accent.
    /// </summary>
    [Parameter] public BitAccentColorConfig? Config { get; set; }

    /// <summary>
    /// The callback that is called when the accent color changes, receiving the applied accent color.
    /// </summary>
    [Parameter] public EventCallback<string> OnChange { get; set; }

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

    protected override void OnParametersSet()
    {
        _config = Config ?? _serviceProvider.GetService<BitAccentColorConfig>();

        base.OnParametersSet();
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
            // Awaited rather than fired off, and awaited even when another instance is the one
            // actually restoring (the service hands every caller the same task): what follows takes
            // the accent state for the answer, so it must not run while the stores are still being
            // read - that is what would ring the default next to the visitor's own swatch.
            await _accentColorService.InitializeAsync(_config);

            // The restore above (or the confirmation that there was nothing to restore) is what makes
            // the C# accent state authoritative for this client, so re-render to hand the active-swatch
            // marking back to it. Rendered rather than left to the CSS marker for good: that marker
            // only covers the accents the host page's BitAccentColorHead was configured with, while
            // this instance may offer its own - and an app that skipped the head altogether still
            // gets a ring.
            _hydrated = true;
            StateHasChanged();
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    private string GetSwatchClass(bool isActive, bool classMarked)
    {
        return string.Join(' ', new[]
        {
            "bit-acs-swt",
            // Until this instance is interactive, a CSS strategy rings the swatch through the marker
            // CSS BitAccentColorHead emits (keyed on the bit-accent root attribute) instead of this
            // class, so prerendered markup - where the C# state is the server's, and a cached
            // response's server never saw this visitor - cannot ring the wrong swatch.
            isActive && classMarked ? "bit-acs-act" : null,
            Classes?.Swatch,
            isActive && classMarked ? Classes?.ActiveSwatch : null
        }.Where(c => c.HasValue()));
    }

    private string GetSwatchStyle(BitAccentColorItem item, bool isActive, bool classMarked)
    {
        return string.Join(';', new[]
        {
            $"--bit-acs-clr:{GetSwatchColor(item.Color)}",
            Styles?.Swatch,
            // Gated exactly like the built-in ring in GetSwatchClass: before this instance is
            // interactive the C# accent is the prerender's, so applying the custom active styling
            // here would mark the wrong swatch on a cached response - the one case the CSS marker
            // exists to get right.
            isActive && classMarked ? Styles?.ActiveSwatch : null
        }.Where(s => s.HasValue()));
    }

    /// <summary>
    /// The color the swatch paints itself with. An accent may be spelled as a bare token everywhere
    /// else in the feature (see <see cref="BitAccentColorSsr.NormalizeToken"/>), but
    /// <c>--bit-acs-clr:8764b8</c> is not a color and the swatch would paint nothing - so the
    /// missing <c>#</c> is supplied. Only that: the author's spelling survives, and a value that is
    /// not hex at all is passed through for CSS to judge.
    /// </summary>
    private static string GetSwatchColor(string? color)
    {
        var value = color?.Trim();
        if (value.HasValue() is false) return string.Empty;

        // The neutral accent is "the packaged palette's own primary" (see BitAccentColorService):
        // picking it clears the overrides, so what it yields is whatever primary the active preset
        // ships - this blue under Fluent, Fluent 2 and Material, Cupertino's system blue. Its swatch
        // therefore paints through --bit-acs-ntr, which the stylesheet sets per packaged preset (this
        // blue by default; the Cupertino bundle retunes it), with the token's own hex as the fallback
        // for a page that loads none of the bundles. Read from a custom property rather than from the
        // theme's --bit-clr-pri, which an applied accent overrides - the point of this swatch is to
        // show the primary underneath that override.
        if (BitAccentColorSsr.NormalizeToken(value) == BitAccentColorSsr.NormalizeToken(BitAccentColorPresets.Blue))
        {
            return $"var(--bit-acs-ntr, {BitAccentColorPresets.Blue})";
        }

        return value!.StartsWith('#') is false && BitAccentColorSsr.NormalizeToken(value) is not null ? $"#{value}" : value;
    }

    private static string GetSwatchAriaLabel(BitAccentColorItem item)
    {
        if (item.AriaLabel.HasValue()) return item.AriaLabel!;

        return item.Name.HasValue() ? $"Apply the {item.Name} accent color" : "Apply this accent color";
    }

    private async Task HandleOnClickAsync(BitAccentColorItem item)
    {
        if (IsEnabled is false) return;

        // The service applies per the app-wide configuration the first InitializeAsync call fixed;
        // this instance's Config is not forwarded, so a differently-configured switcher cannot
        // tear down the stores and attribute its siblings maintain.
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
