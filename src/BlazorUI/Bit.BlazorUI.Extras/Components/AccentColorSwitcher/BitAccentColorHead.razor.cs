namespace Bit.BlazorUI;

/// <summary>
/// BitAccentColorHead is the single-drop first-paint setup for the accent color: place it at the top
/// of the host page's <c>&lt;head&gt;</c> (right after <c>BitThemeSsr.InlineHeadScript</c>, before
/// any stylesheet) and it emits everything the selected <see cref="BitAccentColorFirstPaintStrategy"/>
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
    /// The accent colors whose palettes are emitted. When null, the DefaultAccents (the six
    /// BitAccentColorPresets hues) are used. Keep it in sync with the Accents parameter of the
    /// BitAccentColorSwitcher instances of the app.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAccentColorItem>? Accents { get; set; }

    /// <summary>
    /// The first-paint strategy to emit the head content for; with the default None nothing is
    /// emitted. Keep it in sync with the FirstPaintStrategy parameter of the BitAccentColorSwitcher
    /// instances of the app.
    /// </summary>
    [Parameter] public BitAccentColorFirstPaintStrategy FirstPaintStrategy { get; set; }

    /// <summary>
    /// Optional CSP nonce for the emitted inline script, to satisfy a script-src 'nonce-…'
    /// Content-Security-Policy.
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
    /// The stores the emitted inline script reads the persisted accent from; with the default None
    /// no script is emitted, since nothing is persisted for it to restore. Keep it in sync with the
    /// Persistence parameter of the BitAccentColorSwitcher instances of the app.
    /// </summary>
    [Parameter] public BitAccentColorPersistence Persistence { get; set; }

    /// <summary>
    /// StaticCss strategy only: when set, the all-accents stylesheet is referenced as an external
    /// stylesheet at this href (with the library version appended as a cache-buster) instead of
    /// being inlined - serve BitAccentColorSsr.BuildStaticCss there with long cache headers (see the
    /// AccentColorSwitcher demo page). When null, the stylesheet is inlined into the response, which
    /// needs no endpoint at the cost of re-sending the (well-compressing) palette CSS per page load.
    /// </summary>
    [Parameter] public string? StylesheetHref { get; set; }



    protected override void OnParametersSet()
    {
        _prerenderCss = FirstPaintStrategy is BitAccentColorFirstPaintStrategy.StoredCss
            ? BitAccentColorSsr.BuildPrerenderCss(PersistedAccent, Accents)
            : null;

        base.OnParametersSet();
    }

    private string GetVersionedStylesheetHref()
    {
        var separator = StylesheetHref!.Contains('?') ? '&' : '?';
        return $"{StylesheetHref}{separator}v={BitAccentColorSsr.Version}";
    }
}
