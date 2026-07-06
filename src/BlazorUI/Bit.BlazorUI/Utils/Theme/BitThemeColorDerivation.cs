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
    /// <param name="adjustTextForWcagAa">When true, flips the suggested on-color text if its contrast vs <see cref="BitThemeColorVariants.Main"/> fails WCAG AA for normal text.</param>
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
    /// The variants are derived by shifting only the HSV <em>value</em> (brightness) of
    /// <paramref name="mainHex"/> — hue and saturation are preserved. The result is clamped to the
    /// [0,1] value range, so for an already very bright base color the "light" steps
    /// (<see cref="BitThemeColorVariants.Light"/> / <c>LightHover</c> / <c>LightActive</c>) can
    /// saturate at white and collapse to the same hex, and likewise the "dark" steps can converge
    /// for a near-black base. This is a convenience helper for sparse themes rather than a full
    /// perceptual palette generator; set the variants explicitly when you need precise tints/shades.
    /// </para>
    /// </remarks>
    public static void FillColorRoleFromMain(BitThemeColorVariants? variants, string? mainHex, bool adjustTextForWcagAa = false)
    {
        if (variants is null) return;

        // Reject null/blank/invalid hex up front (same validation as BitThemeColorContrast). An
        // unrecognized value is a no-op rather than a silent all-white derivation, and a validated
        // #RRGGBB value parses cleanly below — so no defensive catch is needed, and a genuine bug
        // (an exception on valid input) is allowed to surface instead of being swallowed.
        if (BitThemeColorContrast.TryNormalizeHex(mainHex, out var normalizedHex) is false) return;

        var baseColor = new BitInternalColor(normalizedHex);
        var (h, s, v) = baseColor.Hsv;

        variants.Main ??= baseColor.Hex;
        variants.MainHover ??= ToHex(h, s, ScaleV(v, 0.96));
        variants.MainActive ??= ToHex(h, s, ScaleV(v, 0.90));
        variants.Dark ??= ToHex(h, s, ScaleV(v, 0.82));
        variants.DarkHover ??= ToHex(h, s, ScaleV(v, 0.76));
        variants.DarkActive ??= ToHex(h, s, ScaleV(v, 0.70));
        variants.Light ??= ToHex(h, s, AddV(v, 0.08));
        variants.LightHover ??= ToHex(h, s, AddV(v, 0.12));
        variants.LightActive ??= ToHex(h, s, AddV(v, 0.16));

        // Track whether Text was auto-generated in this call so we don't overwrite caller-provided values.
        var textWasNull = variants.Text is null;
        variants.Text ??= SuggestOnColorText(baseColor);

        if (adjustTextForWcagAa && textWasNull
            && !string.IsNullOrWhiteSpace(variants.Main)
            && !string.IsNullOrWhiteSpace(variants.Text)
            // A caller-provided Main may be an invalid hex (only auto-filled values are guaranteed valid),
            // and GetContrastRatio throws on non-hex input — validate first so this stays a no-op instead.
            && BitThemeColorContrast.TryNormalizeHex(variants.Main, out _))
        {
            var blackRatio = BitThemeColorContrast.GetContrastRatio("#000000", variants.Main);
            var whiteRatio = BitThemeColorContrast.GetContrastRatio("#FFFFFF", variants.Main);
            var blackPasses = BitThemeColorContrast.MeetsWcagAaNormalText(blackRatio);
            var whitePasses = BitThemeColorContrast.MeetsWcagAaNormalText(whiteRatio);

            // Prefer a candidate that meets WCAG AA; if both pass or neither passes, pick the higher contrast.
            if (blackPasses && !whitePasses)
            {
                variants.Text = "#000000";
            }
            else if (whitePasses && !blackPasses)
            {
                variants.Text = "#FFFFFF";
            }
            else
            {
                variants.Text = blackRatio >= whiteRatio ? "#000000" : "#FFFFFF";
            }
        }
    }

    private static string ToHex(double h, double s, double v, double a = 1)
        => new BitInternalColor(h, s, Clamp01(v), a).Hex!;

    private static double ScaleV(double v, double factor) => Clamp01(v * factor);

    private static double AddV(double v, double delta) => Clamp01(v + delta);

    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

    // Picks black or white on-color text using the same WCAG sRGB relative-luminance contrast as
    // BitThemeColorContrast (rather than the old YIQ 0.299/0.587/0.114 + 0.55 heuristic, which could
    // disagree with the contrast helper and pick the lower-contrast color). We compare the actual
    // contrast ratio of black vs white against the base color and keep whichever is higher, so the
    // default suggestion is consistent with the adjustTextForWcagAa tie-breaker in FillColorRoleFromMain.
    private static string SuggestOnColorText(BitInternalColor c)
    {
        var hex = c.Hex!;
        var blackRatio = BitThemeColorContrast.GetContrastRatio("#000000", hex);
        var whiteRatio = BitThemeColorContrast.GetContrastRatio("#FFFFFF", hex);
        return blackRatio >= whiteRatio ? "#000000" : "#FFFFFF";
    }
}
