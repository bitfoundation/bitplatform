namespace Bit.BlazorUI;

/// <summary>
/// Optional helpers to populate semantic color steps from a single main color (HSV-based). Explicit non-null values on <paramref name="variants"/> are never overwritten.
/// </summary>
public static class BitThemeColorDerivation
{
    /// <summary>Fills unset <see cref="BitThemeColorVariants"/> fields from <paramref name="mainHex"/>.</summary>
    /// <param name="adjustTextForWcagAa">When true, flips suggested on-color text if contrast vs <see cref="BitThemeColorVariants.Main"/> fails WCAG AA for normal text.</param>
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
        catch
        {
            // ignore invalid color strings
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
