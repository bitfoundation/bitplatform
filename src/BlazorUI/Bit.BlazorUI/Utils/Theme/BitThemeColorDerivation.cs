namespace Bit.BlazorUI;

/// <summary>
/// Optional helpers to populate semantic color steps from a single main color (HSV-based).
/// Caller-provided non-null values on the variants object are never overwritten.
/// </summary>
public static class BitThemeColorDerivation
{
    /// <summary>Fills unset <see cref="BitThemeColorVariants"/> fields by deriving HSV-shifted hex values from <paramref name="mainHex"/>.</summary>
    /// <param name="variants">Target variants to fill in-place. Already-populated properties are preserved.</param>
    /// <param name="mainHex">Source color in <c>#RRGGBB</c> form. Whitespace is trimmed; null/empty is a silent no-op.</param>
    /// <param name="adjustTextForWcagAa">When true, flips the suggested on-color text if its contrast vs <see cref="BitThemeColorVariants.Main"/> fails WCAG AA for normal text.</param>
    /// <remarks>
    /// Invalid hex inputs (e.g. <c>"not-a-color"</c>) are tolerated: the underlying
    /// <c>BitInternalColor</c> parser silently falls back to white, and the resulting all-white
    /// derivation is still a usable theme branch. The outer <c>try/catch</c> below is defense-in-depth
    /// for unforeseen exceptions; nothing currently throws past <c>BitInternalColor</c>.
    /// </remarks>
    public static void FillColorRoleFromMain(BitThemeColorVariants? variants, string? mainHex, bool adjustTextForWcagAa = false)
    {
        if (variants is null || string.IsNullOrWhiteSpace(mainHex)) return;

        try
        {
            var baseColor = new BitInternalColor(mainHex.Trim());
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

            if (adjustTextForWcagAa && textWasNull && variants.Main is not null && variants.Text is not null)
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
        catch (Exception)
        {
            // Defense-in-depth: BitInternalColor's parser swallows hex format errors and falls back
            // to white, so this catch is rarely hit in practice. Suppressing here mirrors the
            // public contract documented above (FillColorRoleFromMain never throws on bad input).
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
