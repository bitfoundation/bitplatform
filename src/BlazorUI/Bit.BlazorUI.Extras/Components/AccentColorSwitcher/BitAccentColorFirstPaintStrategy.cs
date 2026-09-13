namespace Bit.BlazorUI;

/// <summary>
/// How the accent palette reaches the very first paint of a page load, before any Blazor runtime is
/// up. After hydration every strategy behaves identically (the palette is applied as runtime CSS
/// variable overrides through <see cref="BitThemeManager"/>); the strategy only decides what a cold
/// browser has to work with, which is what matters when the served HTML comes from a cache (e.g. a
/// CDN) that cannot vary on this visitor's cookie.
/// </summary>
/// <remarks>
/// The CSS strategies restore the accent from the stores <see cref="BitAccentColorPersistence"/>
/// enables, so they require a persistence other than <see cref="BitAccentColorPersistence.None"/>:
/// with nothing persisted there is nothing to restore pre-paint, and the inline head script is not
/// even emitted (see <see cref="BitAccentColorSsr.BuildInlineHeadScriptBody"/>).
/// </remarks>
public enum BitAccentColorFirstPaintStrategy
{
    /// <summary>
    /// No first-paint machinery (the default): the accent is applied only after hydration, so a
    /// server-rendered page paints the packaged palette first and flips to the persisted accent once
    /// the client is up. The preference is still persisted to (and restored from) whatever stores
    /// <see cref="BitAccentColorPersistence"/> enables; no
    /// <c>bit-accent</c> attribute is set, no palette snapshot is kept, and
    /// <see cref="BitAccentColorHead"/> emits nothing. Pick one of the CSS strategies below when
    /// that first-paint flash matters.
    /// </summary>
    None,

    /// <summary>
    /// First paint comes from a static stylesheet holding the palettes of every offered accent, each
    /// scoped to the <c>bit-accent</c> attribute on the root element (see
    /// <see cref="BitAccentColorSsr.BuildStaticCss"/>). The inline head script sets that attribute
    /// from the persisted preference before anything is painted, so the served HTML is
    /// accent-agnostic and safe to cache. This is the recommended strategy when the accents are a
    /// fixed set known at build time.
    /// </summary>
    StaticCss,

    /// <summary>
    /// First paint comes from a snapshot of the generated palette CSS kept in localStorage: every
    /// accent change stores the palette alongside the preference, and the inline head script injects
    /// it as a <c>&lt;style&gt;</c> element before anything is painted. No static stylesheet is
    /// needed, so the accents do not have to be known up front - at the cost of a per-visitor
    /// snapshot that is dropped (falling back to a normal post-hydration repaint) whenever it does
    /// not match the persisted preference or was written by a different library version. The
    /// snapshot lives in localStorage, so this strategy needs the
    /// <see cref="BitAccentColorPersistence.LocalStorage"/> store enabled.
    /// </summary>
    StoredCss,
}
