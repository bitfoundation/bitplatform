namespace Bit.BlazorUI;

/// <summary>
/// Optional helpers to populate semantic color steps from a single main color (OKLCH-based).
/// Caller-provided non-null values on the variants object are never overwritten.
/// </summary>
public static class BitThemeColorDerivation
{
    // Interactive/dark steps: relative OKLab lightness scale at constant hue and chroma
    // (out-of-gamut chroma is reduced by BitThemeOklch). Factors are calibrated against the
    // packaged Fluent light palette (e.g. primary #1A86D8 → hover #197FCD is L × 0.96).
    private const double MainHoverDarken = 0.04;
    private const double MainActiveDarken = 0.075;
    private const double DarkDarken = 0.12;
    private const double DarkHoverDarken = 0.15;
    private const double DarkActiveDarken = 0.18;

    // Light tints: OKLab mix with white (L moves toward 1, chroma scales down by the
    // same factor), again matching the packaged palette, where light/light-hover/light-active are
    // ~58% / ~47.5% / ~37.5% white mixes of main. Note the hover/active steps of the light family
    // step BACK toward main (a tinted background darkens on hover), so Light is the lightest step.
    private const double LightWhiteMix = 0.58;
    private const double LightHoverWhiteMix = 0.475;
    private const double LightActiveWhiteMix = 0.375;

    // Disabled pair (fill + its text), the one family that is NOT a relative step off main: the
    // lightness is an absolute OKLab target and the chroma is capped at a faint hue trace, matching
    // the recipe of the packaged palettes (see the "disabled" note in theme-variables.scss). A
    // relative mix would inherit main's lightness, so a bright role and a dark one would land on
    // different disabled weights and the state would read as a per-role variant instead of one
    // state; an absolute target puts every role's disabled state at the same weight, which is what
    // makes it recognizable. The values are the packaged palettes' targets, so the 8 semantic roles
    // of both Fluent palettes come out of this method byte-identical (pinned by a derivation test).
    private const double DisabledL = 0.94;
    private const double DisabledTextL = 0.69;
    private const double DisabledC = 0.030;
    private const double DisabledTextC = 0.040;

    // Dark-scheme ceilings: for a seed darker than the targets themselves, the absolute values would
    // make the disabled pair LIGHTER than main, i.e. more prominent than the enabled state, so they
    // are capped to a fraction of the seed's own lightness. The two fractions differ so the
    // fill-below-text ordering survives the cap. For every packaged dark role (all of which are
    // bright, as a dark palette's accents must be) the cap is inert and the target applies.
    // The light scheme needs no mirror of this: there the disabled text can legitimately land darker
    // than a pale accent such as warning yellow, whose brightness comes from chroma rather than
    // luminance - dropping the chroma is what marks it disabled, and keeping every role in one
    // lightness band matters more than a luminance ordering that carries no perceptual meaning.
    private const double DarkSchemeDisabledMaxScale = 0.85;
    private const double DarkSchemeDisabledTextMaxScale = 0.95;

    // Dark-scheme steps, calibrated the same way against the packaged Fluent dark palette (whose
    // primary AND secondary families agree on these fractions to within rounding). Interactive
    // states are black mixes (a fill on a dark surface dims on interaction), the dark family keeps
    // chroma while lowering lightness, and the light family still mixes toward white but far less
    // (the dark main is already bright). The disabled pair is not part of this group - it targets the
    // absolute lightness band above.
    private const double DarkSchemeMainHoverBlackMix = 0.075;
    private const double DarkSchemeMainActiveBlackMix = 0.15;
    private const double DarkSchemeDarkDarken = 0.14;
    private const double DarkSchemeDarkHoverDarken = 0.17;
    private const double DarkSchemeDarkActiveDarken = 0.205;
    private const double DarkSchemeLightWhiteMix = 0.28;
    private const double DarkSchemeLightHoverWhiteMix = 0.18;
    private const double DarkSchemeLightActiveWhiteMix = 0.09;
    private const double DarkSchemeDisabledL = 0.30;
    private const double DarkSchemeDisabledTextL = 0.52;

    /// <summary>Fills unset <see cref="BitThemeColorVariants"/> fields by deriving OKLCH-shifted hex values from <paramref name="mainHex"/>.</summary>
    /// <param name="variants">Target variants to fill in-place. Already-populated properties are preserved.</param>
    /// <param name="mainHex">Source color in <c>#RGB</c> or <c>#RRGGBB</c> form. Whitespace is trimmed.</param>
    /// <remarks>
    /// <para>
    /// The method is lenient and never throws on bad input: a null/blank <paramref name="mainHex"/>,
    /// or a value that is not a valid hex color, is treated as "nothing to derive from" and the
    /// call is a no-op (the target <paramref name="variants"/> are left untouched). Validation uses
    /// the same rule as <see cref="BitThemeColorContrast"/> so the two helpers agree on what a valid
    /// color is. Earlier versions fabricated an all-white palette for unrecognized input, which
    /// silently overrode stylesheet defaults with wrong colors and masked the mistake; treating
    /// invalid input as a no-op surfaces it (the role keeps its existing/default colors) without
    /// throwing.
    /// </para>
    /// <para>
    /// Variants are derived in the perceptually uniform OKLCH space from the resolved
    /// <see cref="BitThemeColorVariants.Main"/> color - falling back to <paramref name="mainHex"/>
    /// when a preset <c>Main</c> is not a parseable hex color. The interactive and dark steps
    /// (<c>MainHover</c>/<c>MainActive</c>/<c>Dark</c>/<c>DarkHover</c>/<c>DarkActive</c>) scale
    /// OKLab lightness down at constant hue; the tint steps (<c>Light</c>/<c>LightHover</c>/
    /// <c>LightActive</c>) are white mixes; and the disabled pair (<c>Disabled</c>/
    /// <c>DisabledText</c>) is muted to an absolute lightness with the chroma capped at a faint hue
    /// trace, so a disabled role looks the same whatever its main color is. All step sizes are
    /// calibrated to the packaged Fluent palette, so a derived role behaves like a packaged one -
    /// including the light family's ordering, where <c>Light</c> is the lightest value and its
    /// hover/active states step back toward <c>Main</c>. <see cref="BitThemeColorVariants.Focus"/>
    /// is aliased to the resolved <c>Main</c> (matching <c>--bit-clr-*-focus</c> in the packaged
    /// palettes). Because OKLab lightness is perceptual, the derived interactive fills keep a
    /// ≥ 3:1 WCAG contrast against the auto-selected <c>Text</c> for any seed color (covered by
    /// tests); set the variants explicitly when you need precise tints/shades.
    /// </para>
    /// <para>
    /// The auto-generated <see cref="BitThemeColorVariants.Text"/> (on-color text) is set to whichever
    /// of black/white has the higher WCAG sRGB relative-luminance contrast against the resolved
    /// <see cref="BitThemeColorVariants.Main"/> background - which, for a black/white choice, is also the
    /// WCAG-AA-optimal option. When <c>Main</c> was preset to a value that is not a valid hex color
    /// (e.g. a CSS <c>var()</c> reference), <c>Text</c> is left unset since the actual background color
    /// cannot be evaluated. A caller-provided <c>Text</c> is never overwritten.
    /// </para>
    /// </remarks>
    public static void FillColorRoleFromMain(BitThemeColorVariants? variants, string? mainHex)
    {
        FillColorRoleFromMain(variants, mainHex, BitThemeColorScheme.Light);
    }

    /// <summary>
    /// Scheme-aware variant of <see cref="FillColorRoleFromMain(BitThemeColorVariants?, string?)"/>.
    /// <paramref name="mainHex"/> is the role's main color <em>as it should appear in that scheme</em>
    /// (for a dark scheme, typically a brightened brand color - <see cref="BitThemeFactory.CreateDarkTheme(string)"/>
    /// performs that brightening automatically). <see cref="BitThemeColorScheme.Dark"/> flips the
    /// step directions to the packaged dark palette's conventions: interactive states dim toward
    /// black, tints stay closer to main, and the disabled pair targets the dark palettes' lightness
    /// band (a dim fill with a mid-tone label) instead of the light palettes' pale one.
    /// All other guarantees (no-op on invalid input, presets preserved, contrast-picked
    /// <see cref="BitThemeColorVariants.Text"/>, <c>Focus</c> aliasing <c>Main</c>) are identical.
    /// </summary>
    public static void FillColorRoleFromMain(BitThemeColorVariants? variants, string? mainHex, BitThemeColorScheme scheme)
    {
        if (variants is null) return;

        // Reject null/blank/invalid hex up front (same validation as BitThemeColorContrast). An
        // unrecognized value is a no-op rather than a silent all-white derivation, and a validated
        // #RRGGBB value parses cleanly below - so no defensive catch is needed, and a genuine bug
        // (an exception on valid input) is allowed to surface instead of being swallowed.
        if (BitThemeColorContrast.TryNormalizeHex(mainHex, out var normalizedHex) is false) return;

        var baseColor = new BitInternalColor(normalizedHex);

        variants.Main ??= baseColor.Hex;

        // Derive the shifted variants from the resolved Main (a caller may have preset it to a
        // different color than mainHex, and the variants should form one family with it), falling
        // back to the validated mainHex when the preset Main is not a parseable hex color (e.g. a
        // CSS var() reference) - mirroring how Text resolves its background below.
        var sourceColor = BitThemeColorContrast.TryNormalizeHex(variants.Main, out var mainSourceHex)
            ? new BitInternalColor(mainSourceHex)
            : baseColor;
        var (l, c, h) = BitThemeOklch.FromRgb(sourceColor.R, sourceColor.G, sourceColor.B);

        if (scheme is BitThemeColorScheme.Dark)
        {
            variants.MainHover ??= MixWithBlack(l, c, h, DarkSchemeMainHoverBlackMix);
            variants.MainActive ??= MixWithBlack(l, c, h, DarkSchemeMainActiveBlackMix);
            variants.Dark ??= Darken(l, c, h, DarkSchemeDarkDarken);
            variants.DarkHover ??= Darken(l, c, h, DarkSchemeDarkHoverDarken);
            variants.DarkActive ??= Darken(l, c, h, DarkSchemeDarkActiveDarken);
            variants.Light ??= MixWithWhite(l, c, h, DarkSchemeLightWhiteMix);
            variants.LightHover ??= MixWithWhite(l, c, h, DarkSchemeLightHoverWhiteMix);
            variants.LightActive ??= MixWithWhite(l, c, h, DarkSchemeLightActiveWhiteMix);
            variants.Disabled ??= Muted(Math.Min(DarkSchemeDisabledL, l * DarkSchemeDisabledMaxScale), c, h, DisabledC);
            variants.DisabledText ??= Muted(Math.Min(DarkSchemeDisabledTextL, l * DarkSchemeDisabledTextMaxScale), c, h, DisabledTextC);
        }
        else
        {
            variants.MainHover ??= Darken(l, c, h, MainHoverDarken);
            variants.MainActive ??= Darken(l, c, h, MainActiveDarken);
            variants.Dark ??= Darken(l, c, h, DarkDarken);
            variants.DarkHover ??= Darken(l, c, h, DarkHoverDarken);
            variants.DarkActive ??= Darken(l, c, h, DarkActiveDarken);
            variants.Light ??= MixWithWhite(l, c, h, LightWhiteMix);
            variants.LightHover ??= MixWithWhite(l, c, h, LightHoverWhiteMix);
            variants.LightActive ??= MixWithWhite(l, c, h, LightActiveWhiteMix);
            variants.Disabled ??= Muted(DisabledL, c, h, DisabledC);
            variants.DisabledText ??= Muted(DisabledTextL, c, h, DisabledTextC);
        }

        // The packaged palettes alias the focus indicator to the role's main color
        // (--bit-clr-pri-focus: var(--bit-clr-pri)). Copying the resolved Main string keeps that
        // aliasing even when Main was preset to a non-hex value such as a CSS var() reference.
        variants.Focus ??= variants.Main;

        // On-color text: pick the higher-contrast of black/white against the actual Main background
        // (a caller may have preset Main to a value other than mainHex). When that preset Main is not
        // a valid hex (e.g. a CSS var() reference), the real background color is unknowable here, so
        // Text is left unset for the stylesheet/theme default to apply rather than guessing against a
        // color the user may not actually see. A caller-provided Text is never overwritten.
        if (variants.Text is null && BitThemeColorContrast.TryNormalizeHex(variants.Main, out var mainNormalized))
        {
            variants.Text = SuggestOnColorText(mainNormalized);
        }
    }

    // Multiplicative lightness scaling keeps the steps distinct for any L > 0 (they only converge
    // at pure black), and constant hue/chroma keeps a darkened accent rich instead of muddy.
    private static string Darken(double l, double c, double h, double amount)
        => BitThemeOklch.ToHex(l * (1 - amount), c, h);

    // An OKLab white mix moves lightness toward 1 and scales chroma down by the same factor, so
    // tints stay in-gamut and pastel by construction, and steps stay distinct for any L < 1.
    private static string MixWithWhite(double l, double c, double h, double amount)
        => BitThemeOklch.ToHex(l + ((1 - l) * amount), c * (1 - amount), h);

    // An OKLab black mix scales lightness AND chroma down together (unlike Darken, which keeps
    // chroma), producing the dimmed-not-richer look interactive fills have on a dark surface.
    private static string MixWithBlack(double l, double c, double h, double amount)
        => BitThemeOklch.ToHex(l * (1 - amount), c * (1 - amount), h);

    // Mutes a color to an absolute lightness while keeping its hue and a capped trace of its chroma:
    // the disabled recipe, where the point is that the result does NOT track the role's own
    // lightness (see the DisabledL note above).
    private static string Muted(double targetL, double c, double h, double maxC)
        => BitThemeOklch.ToHex(targetL, Math.Min(c, maxC), h);

    // Picks black or white on-color text using the same WCAG sRGB relative-luminance contrast as
    // BitThemeColorContrast (rather than a YIQ 0.299/0.587/0.114 brightness heuristic, which could
    // disagree with the contrast helper and pick the lower-contrast color). We compare the actual
    // contrast ratio of black vs white against the background and keep whichever is higher; for a
    // black/white choice the higher-contrast option is also the WCAG-AA-optimal one.
    private static string SuggestOnColorText(string backgroundHex)
    {
        var blackRatio = BitThemeColorContrast.GetContrastRatio("#000000", backgroundHex);
        var whiteRatio = BitThemeColorContrast.GetContrastRatio("#FFFFFF", backgroundHex);
        return blackRatio >= whiteRatio ? "#000000" : "#FFFFFF";
    }
}
