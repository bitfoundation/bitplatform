namespace Bit.BlazorUI;

/// <summary>
/// The app-wide configuration of the accent color feature: which accents are offered, how the
/// picked one is persisted, and how it reaches the first paint. The host page's
/// <see cref="BitAccentColorHead"/> and every <see cref="BitAccentColorSwitcher"/> are the head and
/// body halves of one mechanism, so they have to agree on these values - state them once, in code
/// both the server and the client compile: either register the configuration in DI through the
/// accentColor option of AddBitBlazorUIExtrasServices (in the service-registration method the
/// server and client Program.cs share), which every component falls back to when no Config
/// parameter is handed to it, or define one shared instance (e.g. a static field in a shared
/// project) and pass that same instance to each of them. Sharing one instance makes divergence
/// inexpressible. On the runtime side the values are fixed app-wide by the first
/// <see cref="BitAccentColorService.InitializeAsync"/> call.
/// </summary>
public class BitAccentColorConfig
{
    /// <summary>
    /// The accent colors of the app: the swatches the switchers offer, the palettes the head
    /// emits, and the list a persisted value is validated against on restore. When null, the
    /// <see cref="BitAccentColorSwitcher.DefaultAccents"/> (the six
    /// <see cref="BitAccentColorPresets"/> hues) are used.
    /// </summary>
    public IReadOnlyList<BitAccentColorItem>? Accents { get; set; }

    /// <summary>
    /// How the accent palette reaches the very first paint of a page load, before any Blazor
    /// runtime is up: None (the default) applies the accent after hydration only, StaticCss keys a
    /// prebuilt all-accents stylesheet on the bit-accent root attribute, StoredCss keeps a snapshot
    /// of the generated palette CSS in localStorage. The CSS strategies restore from the stores
    /// <see cref="Persistence"/> enables, so they need a persistence other than None to have any
    /// effect.
    /// </summary>
    public BitAccentColorFirstPaintStrategy FirstPaintStrategy { get; set; }

    /// <summary>
    /// The stores the picked accent is persisted to: LocalStorage, Cookie, or both (All); None
    /// (the default) keeps the accent for the current session only. The cookie half is what lets
    /// the server read the preference while prerendering (SSR) - see <see cref="BitAccentColorSsr"/> -
    /// so enable it when the server takes part in painting or seeding the accent.
    /// </summary>
    public BitAccentColorPersistence Persistence { get; set; }
}
