namespace Bit.BlazorUI;

/// <summary>
/// One-color theme quick start: builds a sparse <see cref="BitTheme"/> overlay in which the accent
/// role(s) are fully derived (all 13 slots: main/hover/active, dark and light families, disabled,
/// focus, and a contrast-picked on-color text) from one brand color per role, in the perceptually
/// uniform OKLCH space with step sizes calibrated to the packaged Fluent palettes.
/// </summary>
/// <remarks>
/// <para>
/// The returned theme only sets the seeded accent roles - every other token (neutrals, surfaces,
/// typography, ...) is left null so the packaged stylesheet defaults keep applying. Apply the
/// overlay via <see cref="BitThemeManager.ApplyBitThemeAsync"/> or scope it with a
/// <see cref="BitThemeProvider"/>.
/// </para>
/// <para>
/// Pass the SAME brand color to <see cref="CreateLightTheme(string)"/> and
/// <see cref="CreateDarkTheme(string)"/>: the dark factory first brightens the brand toward white
/// (constant hue) so the accent keeps its identity but reads correctly on a dark surface - the
/// same relationship the packaged Fluent light/dark palettes have (light primary
/// <c>#1276C6</c> → dark primary <c>#4FA3F4</c>). To control the dark-scheme main yourself,
/// derive directly with
/// <see cref="BitThemeColorDerivation.FillColorRoleFromMain(BitThemeColorVariants?, string?, BitThemeColorScheme)"/>.
/// </para>
/// </remarks>
public static class BitThemeFactory
{
    // 0.32 reproduces the packaged primary #1276C6 → #4FA3F4 to within rounding. Unlike the earlier
    // palettes, the packaged light and dark mains are no longer one constant white mix apart across
    // every role (each scheme anchors its own mains against its own surface: the light mains are set
    // so white text on them clears 4.5:1, the dark mains so near-black text does), so this single
    // fraction tracks primary and lands within a few lightness points on the other accent hues.
    private const double DarkSchemeMainWhiteMix = 0.32;

    /// <summary>Builds a light-scheme theme overlay whose primary role is derived from <paramref name="accentHex"/>.</summary>
    /// <param name="accentHex">The brand color in <c>#RGB</c> or <c>#RRGGBB</c> form.</param>
    /// <exception cref="ArgumentException"><paramref name="accentHex"/> is not a valid hex color.</exception>
    public static BitTheme CreateLightTheme(string accentHex)
    {
        return CreateLightTheme(new BitThemeAccentColors { Primary = accentHex });
    }

    /// <summary>Builds a dark-scheme theme overlay whose primary role is derived from <paramref name="accentHex"/> (the same brand color you would pass to <see cref="CreateLightTheme(string)"/>).</summary>
    /// <param name="accentHex">The brand color in <c>#RGB</c> or <c>#RRGGBB</c> form.</param>
    /// <exception cref="ArgumentException"><paramref name="accentHex"/> is not a valid hex color.</exception>
    public static BitTheme CreateDarkTheme(string accentHex)
    {
        return CreateDarkTheme(new BitThemeAccentColors { Primary = accentHex });
    }

    /// <summary>Builds a light-scheme theme overlay deriving one full role per non-null seed in <paramref name="accents"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="accents"/> is null.</exception>
    /// <exception cref="ArgumentException">A provided seed is not a valid hex color.</exception>
    public static BitTheme CreateLightTheme(BitThemeAccentColors accents)
    {
        return Create(accents, BitThemeColorScheme.Light);
    }

    /// <summary>Builds a dark-scheme theme overlay deriving one full role per non-null seed in <paramref name="accents"/> (brand colors, brightened for the dark surface automatically).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="accents"/> is null.</exception>
    /// <exception cref="ArgumentException">A provided seed is not a valid hex color.</exception>
    public static BitTheme CreateDarkTheme(BitThemeAccentColors accents)
    {
        return Create(accents, BitThemeColorScheme.Dark);
    }

    private static BitTheme Create(BitThemeAccentColors accents, BitThemeColorScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(accents);

        var theme = new BitTheme();

        FillRole(theme.Color.Primary, accents.Primary, nameof(accents.Primary), scheme);
        FillRole(theme.Color.Secondary, accents.Secondary, nameof(accents.Secondary), scheme);
        FillRole(theme.Color.Tertiary, accents.Tertiary, nameof(accents.Tertiary), scheme);
        FillRole(theme.Color.Info, accents.Info, nameof(accents.Info), scheme);
        FillRole(theme.Color.Success, accents.Success, nameof(accents.Success), scheme);
        FillRole(theme.Color.Warning, accents.Warning, nameof(accents.Warning), scheme);
        FillRole(theme.Color.SevereWarning, accents.SevereWarning, nameof(accents.SevereWarning), scheme);
        FillRole(theme.Color.Error, accents.Error, nameof(accents.Error), scheme);

        return theme;
    }

    private static void FillRole(BitThemeColorVariants variants, string? seedHex, string seedName, BitThemeColorScheme scheme)
    {
        if (seedHex is null) return;

        // Unlike the lenient FillColorRoleFromMain (which no-ops so sparse/partial data can flow
        // through it), the factory takes explicit caller input - a typo'd brand color must fail
        // loudly rather than silently produce a theme without the requested role.
        if (BitThemeColorContrast.TryNormalizeHex(seedHex, out var normalizedHex) is false)
        {
            throw new ArgumentException(
                $"'{seedHex}' is not a valid hex color for '{seedName}'. Expected '#RGB' or '#RRGGBB'.",
                seedName);
        }

        var mainHex = scheme is BitThemeColorScheme.Dark ? BrightenForDarkScheme(normalizedHex) : normalizedHex;

        BitThemeColorDerivation.FillColorRoleFromMain(variants, mainHex, scheme);
    }

    private static string BrightenForDarkScheme(string brandHex)
    {
        var brand = new BitInternalColor(brandHex);
        var (l, c, h) = BitThemeOklch.FromRgb(brand.R, brand.G, brand.B);

        // White-mix the lightness only: chroma is kept (the gamut mapping in BitThemeOklch trims
        // what sRGB can't hold) so the dark-scheme accent stays as vivid as the brand allows.
        return BitThemeOklch.ToHex(l + ((1 - l) * DarkSchemeMainWhiteMix), c, h);
    }
}

/// <summary>
/// Per-role brand seeds for <see cref="BitThemeFactory"/>. Only non-null roles are derived; the
/// rest keep the packaged stylesheet defaults.
/// </summary>
public class BitThemeAccentColors
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Tertiary { get; set; }
    public string? Info { get; set; }
    public string? Success { get; set; }
    public string? Warning { get; set; }
    public string? SevereWarning { get; set; }
    public string? Error { get; set; }
}
