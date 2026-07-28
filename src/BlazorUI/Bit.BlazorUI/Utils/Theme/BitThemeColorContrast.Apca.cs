namespace Bit.BlazorUI;

/// <summary>
/// APCA (Advanced Perceptual Contrast Algorithm) contrast helpers - the perceptually-tuned,
/// polarity-aware companion to the WCAG 2.x ratios in the other half of this class.
/// </summary>
/// <remarks>
/// <para>
/// WCAG 2.x contrast ratios are known to overstate contrast for dark color pairs, so a pair that
/// "passes" 4.5:1 can be hard to read in a dark theme. APCA (the candidate contrast method for the
/// in-progress WCAG 3) fixes this: it is <b>polarity-aware</b> (dark-text-on-light is scored
/// differently from light-text-on-dark) and its output tracks perceived readability across the
/// whole lightness range. Use it as an <b>advisory</b> signal - especially when tuning dark
/// palettes - while WCAG 2.x remains the formal conformance bar (WCAG 3 is still a Working Draft).
/// </para>
/// <para>
/// Implements the APCA-W3 0.1.9 ("0.98G-4g") constants. The output is a signed lightness-contrast
/// value <c>Lc</c> in roughly <c>[-108, 106]</c>: a <b>positive</b> Lc means dark text on a lighter
/// background, a <b>negative</b> Lc means light text on a darker background. Evaluate readability
/// against the magnitude (<c>|Lc|</c>); the sign only tells you the polarity. Because APCA is
/// directional, <see cref="GetApcaContrast"/> is NOT symmetric in its arguments - pass the text
/// color first and the background second.
/// </para>
/// </remarks>
public static partial class BitThemeColorContrast
{
    // APCA-W3 0.1.9 "0.98G-4g" constants (Myndex). Kept in lockstep with the WCAG-side luminance
    // coefficients' source of truth but intentionally separate: APCA uses a simple power-curve TRC
    // (not the piecewise sRGB linearization WCAG uses), which is part of what makes it perceptual.
    private const double ApcaMainTrc = 2.4;
    private const double ApcaRco = 0.2126729;
    private const double ApcaGco = 0.7151522;
    private const double ApcaBco = 0.0721750;
    private const double ApcaNormBg = 0.56;
    private const double ApcaNormTxt = 0.57;
    private const double ApcaRevTxt = 0.62;
    private const double ApcaRevBg = 0.65;
    private const double ApcaBlackThreshold = 0.022;
    private const double ApcaBlackClamp = 1.414;
    private const double ApcaScale = 1.14;
    private const double ApcaLoOffset = 0.027;
    private const double ApcaDeltaYMin = 0.0005;
    private const double ApcaLoClip = 0.1;

    /// <summary>
    /// Advisory <c>|Lc|</c> threshold for body text (APCA guidance ≈ Lc 75 - the minimum for
    /// fluent reading of normal-weight body copy).
    /// </summary>
    public const double ApcaBodyTextLc = 75.0;

    /// <summary>
    /// Advisory <c>|Lc|</c> threshold for large or bold text (APCA guidance ≈ Lc 60).
    /// </summary>
    public const double ApcaLargeTextLc = 60.0;

    /// <summary>
    /// Advisory <c>|Lc|</c> threshold for non-text UI elements and spot / incidental text (APCA
    /// guidance ≈ Lc 45; the rough analog of WCAG 1.4.11's 3:1 non-text contrast).
    /// </summary>
    public const double ApcaNonTextLc = 45.0;

    /// <summary>
    /// Returns the APCA lightness-contrast <c>Lc</c> of <paramref name="textHex"/> over
    /// <paramref name="backgroundHex"/>. Positive when the text is darker than the background,
    /// negative when it is lighter; evaluate readability against the magnitude. The result is
    /// <c>0</c> when the two colors are effectively equal.
    /// </summary>
    /// <param name="textHex">The text/foreground color as <c>#RGB</c> or <c>#RRGGBB</c>.</param>
    /// <param name="backgroundHex">The background color as <c>#RGB</c> or <c>#RRGGBB</c>.</param>
    /// <exception cref="ArgumentException">
    /// When either input is null, empty, whitespace, or not a <c>#RGB</c>/<c>#RRGGBB</c> hex string.
    /// </exception>
    public static double GetApcaContrast(string textHex, string backgroundHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textHex);
        ArgumentException.ThrowIfNullOrWhiteSpace(backgroundHex);

        if (TryNormalizeHex(textHex, out var txtHex) is false)
        {
            throw new ArgumentException(
                $"'{textHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.", nameof(textHex));
        }

        if (TryNormalizeHex(backgroundHex, out var bgHex) is false)
        {
            throw new ArgumentException(
                $"'{backgroundHex}' is not a valid hex color. Expected '#RGB' or '#RRGGBB'.", nameof(backgroundHex));
        }

        var text = new BitInternalColor(txtHex);
        var background = new BitInternalColor(bgHex);

        return ApcaContrast(ApcaLuminance(text), ApcaLuminance(background));
    }

    /// <summary>Returns true when <c>|Lc|</c> meets the advisory body-text threshold (<see cref="ApcaBodyTextLc"/>).</summary>
    public static bool MeetsApcaBodyText(double lc) => Math.Abs(lc) >= ApcaBodyTextLc;

    /// <summary>Returns true when <c>|Lc|</c> meets the advisory large/bold-text threshold (<see cref="ApcaLargeTextLc"/>).</summary>
    public static bool MeetsApcaLargeText(double lc) => Math.Abs(lc) >= ApcaLargeTextLc;

    /// <summary>Returns true when <c>|Lc|</c> meets the advisory non-text / UI-element threshold (<see cref="ApcaNonTextLc"/>).</summary>
    public static bool MeetsApcaNonText(double lc) => Math.Abs(lc) >= ApcaNonTextLc;

    private static double ApcaLuminance(BitInternalColor color)
    {
        return (ApcaRco * ApcaChannel(color.R))
             + (ApcaGco * ApcaChannel(color.G))
             + (ApcaBco * ApcaChannel(color.B));
    }

    private static double ApcaChannel(byte channel) => Math.Pow(channel / 255.0, ApcaMainTrc);

    private static double ApcaContrast(double textY, double backgroundY)
    {
        // Soft-clamp very dark luminances so near-black pairs don't produce runaway contrast.
        textY = SoftClampBlack(textY);
        backgroundY = SoftClampBlack(backgroundY);

        // Colors too close in luminance to matter.
        if (Math.Abs(backgroundY - textY) < ApcaDeltaYMin) return 0.0;

        double sapc, output;
        if (backgroundY > textY)
        {
            // Normal polarity: dark text on a lighter background → positive Lc.
            sapc = (Math.Pow(backgroundY, ApcaNormBg) - Math.Pow(textY, ApcaNormTxt)) * ApcaScale;
            output = sapc < ApcaLoClip ? 0.0 : sapc - ApcaLoOffset;
        }
        else
        {
            // Reverse polarity: light text on a darker background → negative Lc.
            sapc = (Math.Pow(backgroundY, ApcaRevBg) - Math.Pow(textY, ApcaRevTxt)) * ApcaScale;
            output = sapc > -ApcaLoClip ? 0.0 : sapc + ApcaLoOffset;
        }

        return output * 100.0;
    }

    private static double SoftClampBlack(double y)
    {
        return y > ApcaBlackThreshold ? y : y + Math.Pow(ApcaBlackThreshold - y, ApcaBlackClamp);
    }
}
