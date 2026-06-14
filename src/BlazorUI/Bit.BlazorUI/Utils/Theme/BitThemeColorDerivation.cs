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
    /// The method is lenient and never throws on bad input: a null/blank <paramref name="mainHex"/>,
    /// or a value that is not a valid hex color, is treated as "nothing to derive from" and the
    /// call is a no-op (the target <paramref name="variants"/> are left untouched). Validation uses
    /// the same rule as <see cref="BitThemeColorContrast"/> so the two helpers agree on what a valid
    /// color is. Earlier versions fabricated an all-white palette for unrecognized input, which
    /// silently overrode stylesheet defaults with wrong colors and masked the mistake; treating
    /// invalid input as a no-op surfaces it (the role keeps its existing/default colors) without
    /// throwing.
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
            && !string.IsNullOrWhiteSpace(variants.Text))
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

    private static string SuggestOnColorText(BitInternalColor c)
    {
        var lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        return lum > 0.55 ? "#000000" : "#FFFFFF";
    }
}
