namespace Bit.BlazorUI;

/// <summary>
/// One theme preset as C# knows it: the <c>bit-theme</c> token, plus the facts about it that code has
/// to know BEFORE its stylesheet can answer - today the two page surfaces a host page paints the
/// browser chrome with at first paint (see <see cref="BitThemeHead"/>).
/// </summary>
/// <remarks>
/// <para>
/// A preset is DECLARED by whoever ships its stylesheet: the core package registers the Fluent family
/// its own stylesheet carries, Bit.BlazorUI.Extras registers the Fluent 2, Material and Cupertino ones
/// it carries, and an app registers its own alongside them with
/// <see cref="BitThemePresetRegistry.Register(BitThemePreset)"/>. Everything that reads presets -
/// <see cref="BitThemeSurfaces"/>, and through it <see cref="BitThemeHead"/> - reads the registry, so
/// a preset added by any of the three is a first-class one from the moment it registers.
/// </para>
/// <para>
/// Only what cannot be read off the live page belongs here. Everything else about a theme is in its
/// <c>--bit-*</c> tokens, where an app's own overrides and a picked accent are already reflected.
/// </para>
/// </remarks>
public sealed class BitThemePreset
{
    /// <summary>
    /// The <c>bit-theme</c> attribute value, e.g. <c>"material-dark"</c>. Normalized on registration
    /// (trim + lower-case invariant) and validated as a <c>[a-z0-9-]</c> token of at most 64
    /// characters, exactly as <see cref="BitThemeName.Custom(string)"/> validates one.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The preset's <c>--bit-clr-bg-pri</c> (the page background) as a hex literal, or
    /// <see langword="null"/> when this preset should not appear in
    /// <see cref="BitThemeSurfaces.BackgroundPrimary"/>.
    /// </summary>
    /// <remarks>
    /// A literal here is a restatement of what the stylesheet already declares, and it is restated
    /// because a host page needs it while the document is still being parsed - before the CSS that
    /// carries it has loaded. The packaged presets pin theirs to their palettes with a contract test.
    /// </remarks>
    public string? BackgroundPrimary { get; init; }

    /// <summary>
    /// The preset's <c>--bit-clr-bg-sec</c> (the surface cards and panels sit on) as a hex literal,
    /// or <see langword="null"/> when this preset should not appear in
    /// <see cref="BitThemeSurfaces.BackgroundSecondary"/>. Same first-paint purpose as
    /// <see cref="BackgroundPrimary"/>, for an app whose own pages are drawn on the secondary surface.
    /// </summary>
    public string? BackgroundSecondary { get; init; }

    /// <summary>
    /// Whether this preset paints a dark page, by the "ends with dark" rule the packaged stylesheets,
    /// the first-paint script and the runtime client all classify names with - so every layer puts a
    /// given name on the same side of light / dark.
    /// </summary>
    /// <remarks>
    /// Case-insensitive, while those other layers test the attribute value with an ordinal
    /// <c>/dark$/</c>. They only ever see a name that has been through
    /// <see cref="BitThemeName.NormalizeToken"/> and so is already lower-case; this property can be
    /// read off an instance that has not been registered yet, whose <see cref="Name"/> is whatever
    /// was written - <c>"Acme-Dark"</c> - and which normalization would lower-case to a name those
    /// layers do call dark. Ignoring case is what keeps the two answers the same.
    /// </remarks>
    public bool IsDark => Name.EndsWith("dark", StringComparison.OrdinalIgnoreCase);

    /// <summary>The theme name, for the APIs that take one.</summary>
    public BitThemeName ThemeName => BitThemeName.Custom(Name);

    public override string ToString() => Name;
}
