using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

/// <summary>
/// BitAccentColorHead is the single-drop first-paint setup for the accent color: place it at the top
/// of the host page's <c>&lt;head&gt;</c> (right after <c>BitThemeSsr.InlineHeadScript</c>, before
/// any stylesheet) and it emits everything the strategy selected by <see cref="Config"/>
/// needs - the inline script that re-resolves the accent from localStorage / the preference cookie
/// before anything is painted (which is what keeps the accent correct when the HTML comes out of a
/// cache), plus the palette CSS: the all-accents stylesheet in
/// <see cref="BitAccentColorFirstPaintStrategy.StaticCss"/> mode, or the persisted accent's
/// per-request style in <see cref="BitAccentColorFirstPaintStrategy.StoredCss"/> mode. With the
/// default <see cref="BitAccentColorFirstPaintStrategy.None"/> there is no first-paint machinery to
/// set up, so it emits nothing. Renders no element of its own, so it does not inherit
/// <see cref="BitComponentBase"/>.
/// </summary>
/// <remarks>
/// The component intentionally renders nothing cookie-dependent in
/// <see cref="BitAccentColorFirstPaintStrategy.StaticCss"/> mode, so the whole response stays
/// identical for every visitor and safe to cache; the inline script is what personalizes it, before
/// first paint. In <see cref="BitAccentColorFirstPaintStrategy.StoredCss"/> mode pass
/// <see cref="PersistedAccent"/> (the accent cookie's value) so origin-rendered responses paint
/// immediately; cached responses are covered by the localStorage snapshot the strategy keeps.
/// </remarks>
public partial class BitAccentColorHead : ComponentBase
{
    private string? _prerenderCss;

    /// <summary>
    /// The configuration this instance emits the head half for: the Config parameter when one is
    /// handed in, otherwise the app-wide instance registered in DI (the accentColor option of
    /// AddBitBlazorUIExtrasServices), otherwise null (nothing is emitted).
    /// </summary>
    private BitAccentColorConfig? _config;



    [Inject] private IServiceProvider _serviceProvider { get; set; } = default!;



    /// <summary>
    /// The app-wide accent configuration this component emits the head half of: its
    /// FirstPaintStrategy selects what is emitted (nothing for the default None), its Persistence
    /// selects the stores the inline script reads, and its Accents are the palettes emitted in
    /// StaticCss mode. When null, the BitAccentColorConfig registered in DI (the accentColor option
    /// of AddBitBlazorUIExtrasServices) is used - registering it in a service-registration method
    /// both the server and the client compile, or handing this parameter the same instance the
    /// app's BitAccentColorSwitcher instances use, is what keeps the head and the switchers
    /// describing one configuration. With neither (or with a None strategy), nothing is emitted.
    /// </summary>
    [Parameter] public BitAccentColorConfig? Config { get; set; }

    /// <summary>
    /// Optional CSP nonce for the emitted inline script and the emitted palette style element, to
    /// satisfy a script-src / style-src 'nonce-…' Content-Security-Policy. The inline script stamps
    /// the same nonce onto the style element it injects from the localStorage snapshot, so the whole
    /// first-paint chain passes under one nonce.
    /// </summary>
    [Parameter] public string? Nonce { get; set; }

    /// <summary>
    /// The persisted accent preference, usually the BitAccentColorNames.CookieName cookie's value
    /// read from the request. Only used by the StoredCss strategy, whose origin-rendered responses
    /// paint the accent through a per-request style; the StaticCss strategy is deliberately
    /// cookie-independent. A missing or tampered value is treated as "no preference".
    /// </summary>
    [Parameter] public string? PersistedAccent { get; set; }

    /// <summary>
    /// StaticCss strategy only: when set, the all-accents stylesheet is referenced as an external
    /// stylesheet at this href (with the library version appended as a cache-buster) instead of
    /// being inlined - serve BitAccentColorSsr.BuildStaticCss there with long cache headers (see the
    /// AccentColorSwitcher demo page). Use a root-relative href (e.g. "/accent-colors.css"): this
    /// component sits at the top of head, before any base element, so a relative href would resolve
    /// against the current page path and silently 404 on every non-root route. When null, the
    /// stylesheet is inlined into the response, which needs no endpoint at the cost of re-sending
    /// the (well-compressing) palette CSS per page load.
    /// </summary>
    [Parameter] public string? StylesheetHref { get; set; }



    protected override void OnParametersSet()
    {
        _config = Config ?? _serviceProvider.GetService<BitAccentColorConfig>();

        _prerenderCss = _config?.FirstPaintStrategy is BitAccentColorFirstPaintStrategy.StoredCss
            ? BitAccentColorSsr.BuildPrerenderCss(PersistedAccent, _config?.Accents)
            : null;

        base.OnParametersSet();
    }

    private string GetVersionedStylesheetHref()
    {
        var separator = StylesheetHref!.Contains('?') ? '&' : '?';
        return $"{StylesheetHref}{separator}v={BitAccentColorSsr.Version}";
    }
}
