namespace Bit.BlazorUI;

/// <summary>
/// Single source of truth for the attribute, storage and element names the accent-color feature uses.
/// Keep these aligned with <c>BitAccentColor.ts</c> and <see cref="BitAccentColorSsr"/> - the inline
/// first-paint script and the runtime client read the same stores, so a rename here without the
/// matching TypeScript change silently breaks persistence.
/// </summary>
public static class BitAccentColorNames
{
    /// <summary>
    /// The attribute set on the root <c>&lt;html&gt;</c> element carrying the active accent token
    /// (the accent's hex color without the leading <c>#</c>, lower-cased, e.g. <c>8764b8</c>).
    /// The stylesheet produced by <see cref="BitAccentColorSsr.BuildStaticCss"/> scopes each
    /// accent palette to this attribute, which is what makes the
    /// <see cref="BitAccentColorFirstPaintStrategy.StaticCss"/> strategy paint correctly before hydration.
    /// </summary>
    public const string Attribute = "bit-accent";

    /// <summary>
    /// The attribute a <see cref="BitAccentColorSwitcher"/> swatch carries its own accent token in,
    /// which is what lets <see cref="BitAccentColorSsr.BuildSwatchMarkerCss"/> ring the swatch
    /// matching <see cref="Attribute"/> before hydration. Only set under a CSS first-paint strategy,
    /// where that marker CSS is emitted.
    /// </summary>
    public const string SwatchAttribute = "bit-accent-swatch";

    /// <summary>
    /// The localStorage key holding the persisted accent token.
    /// </summary>
    public const string StorageKey = "bit-accent-color";

    /// <summary>
    /// The cookie mirroring <see cref="StorageKey"/>. localStorage is unreachable while the server
    /// prerenders, so the accent is written to a cookie as well, letting the server paint the
    /// accented palette into the first response - see
    /// <see cref="BitAccentColorSsr.BuildRootAccentAttributes"/> and
    /// <see cref="BitAccentColorSsr.BuildPrerenderCss"/>. Named after the storage key so the two
    /// stores are recognizably one preference.
    /// </summary>
    public const string CookieName = StorageKey;

    /// <summary>
    /// The localStorage key holding the generated accent palette CSS snapshot that the
    /// <see cref="BitAccentColorFirstPaintStrategy.StoredCss"/> strategy keeps, so the inline head script can paint
    /// the accent before hydration even when the served HTML comes from a cache that knows nothing
    /// about this visitor's cookie.
    /// </summary>
    public const string CssStorageKey = "bit-accent-css";

    /// <summary>
    /// Id of the <c>&lt;style&gt;</c> element carrying the accent palette before hydration - either
    /// emitted by the server from <see cref="BitAccentColorSsr.BuildPrerenderCss"/> or injected by
    /// the inline head script from the stored snapshot. The runtime client finds, updates and
    /// removes the element through this id.
    /// </summary>
    public const string StyleElementId = "bit-accent-css";

    /// <summary>
    /// Marks a server-emitted <see cref="StyleElementId"/> style with the accent token it was built
    /// for, so <see cref="BitAccentColorSsr.PrerenderCssGuardScript"/> can tell it apart from the
    /// snapshot the inline head script injects and drop it when it does not match the accent that
    /// script resolved - see <see cref="BitAccentColorSsr.BuildPrerenderCssGuardScript"/>.
    /// </summary>
    public const string StyleAccentAttribute = "data-bit-accent";
}
