namespace Bit.BlazorUI;

/// <summary>
/// Optional helpers to populate semantic color steps from a single main color (HSV-based).
/// Caller-provided non-null values on the variants object are never overwritten.
/// </summary>
public static class BitThemeColorDerivation
{
    /// <summary>Fills unset <see cref="BitThemeColorVariants"/> fields by deriving HSV-shifted hex values from <paramref name="mainHex"/>.</summary>
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
    /// The variants are derived by shifting only the HSV <em>value</em> (brightness) of the resolved
    /// <see cref="BitThemeColorVariants.Main"/> color - falling back to <paramref name="mainHex"/>
    /// when a preset <c>Main</c> is not a parseable hex color - hue and saturation are preserved. The result is clamped to the
    /// [0,1] value range, so for an already very bright base color the "light" steps
    /// (<see cref="BitThemeColorVariants.Light"/> / <c>LightHover</c> / <c>LightActive</c>) can
    /// saturate at white and collapse to the same hex, and likewise the "dark" steps can converge
    /// for a near-black base. This is a convenience helper for sparse themes rather than a full
    /// perceptual palette generator; set the variants explicitly when you need precise tints/shades.
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
        if (variants is null) return;

        // Reject null/blank/invalid hex up front (same validation as BitThemeColorContrast). An
        // unrecognized value is a no-op rather than a silent all-white derivation, and a validated
        // #RRGGBB value parses cleanly below - so no defensive catch is needed, and a genuine bug
        // (an exception on valid input) is allowed to surface instead of being swallowed.
        if (BitThemeColorContrast.TryNormalizeHex(mainHex, out var normalizedHex) is false) return;

        var baseColor = new BitInternalColor(normalizedHex);

        variants.Main ??= baseColor.Hex;

        // Derive the brightness-shifted variants from the resolved Main (a caller may have preset it
        // to a different color than mainHex, and the variants should form one family with it),
        // falling back to the validated mainHex when the preset Main is not a parseable hex color
        // (e.g. a CSS var() reference) - mirroring how Text resolves its background below.
        var sourceColor = BitThemeColorContrast.TryNormalizeHex(variants.Main, out var mainSourceHex)
            ? new BitInternalColor(mainSourceHex)
            : baseColor;
        var (h, s, v) = sourceColor.Hsv;

        variants.MainHover ??= ToHex(h, s, ScaleV(v, 0.96));
        variants.MainActive ??= ToHex(h, s, ScaleV(v, 0.90));
        variants.Dark ??= ToHex(h, s, ScaleV(v, 0.82));
        variants.DarkHover ??= ToHex(h, s, ScaleV(v, 0.76));
        variants.DarkActive ??= ToHex(h, s, ScaleV(v, 0.70));
        variants.Light ??= ToHex(h, s, AddV(v, 0.08));
        variants.LightHover ??= ToHex(h, s, AddV(v, 0.12));
        variants.LightActive ??= ToHex(h, s, AddV(v, 0.16));

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

    private static string ToHex(double h, double s, double v, double a = 1)
        => new BitInternalColor(h, s, Clamp01(v), a).Hex!;

    private static double ScaleV(double v, double factor) => Clamp01(v * factor);

    private static double AddV(double v, double delta) => Clamp01(v + delta);

    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

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
