namespace Bit.BlazorUI;

/// <summary>
/// Tuning knobs for <see cref="BitThemeFactory.CreateLightThemeFromSeed(string, BitThemeSeedOptions?)"/>
/// and its dark counterpart. Every default is "change nothing beyond the hue rotation", so seeding the
/// packaged palettes' own primary reproduces them exactly and each knob is a deliberate move away
/// from that.
/// </summary>
public class BitThemeSeedOptions
{
    /// <summary>
    /// Extra OKLCH chroma given to the neutral families (surfaces, foregrounds, borders, the gray
    /// ramp) at the seed's hue - how strongly the whole UI is tinted by the brand color. Defaults to
    /// <c>0</c>: the neutrals keep exactly the chroma the packaged palette gave them, which is none at
    /// all in the light palette and about <c>0.012</c> in the dark one, and only turn onto the new hue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is why a seeded dark theme has tinted surfaces out of the box while a seeded light theme's
    /// stay gray: the packaged dark palette already tints its neutrals toward the primary and the
    /// light one does not, and the default is to preserve what each palette decided. Set this to give
    /// the light scheme the same treatment.
    /// </para>
    /// <para>
    /// Around <c>0.004</c>-<c>0.012</c> reads as "warm/cool gray"; above roughly <c>0.03</c> the
    /// surfaces stop reading as neutral and start reading as colored. The tint is applied at constant
    /// OKLCH lightness, so it moves perceived hue without moving the contrast ratios the tiers are
    /// solved for.
    /// </para>
    /// </remarks>
    public double? NeutralTintChroma { get; set; }

    /// <summary>
    /// How far, in OKLCH degrees, a conventional status hue (success green, warning amber, error red)
    /// may be rotated toward the seed's hue. The actual rotation is <c>min(hueDistance / 2, this)</c>
    /// in whichever direction is shorter - Material 3's <c>Blend.harmonize</c> rule. Defaults to
    /// <c>0</c>: the status roles keep the packaged hues, and only their neighbours in the theme move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rotating status colors all the way onto the seed is deliberately not offered at any setting:
    /// "error" is red because of a convention older than the app, and a blue error is a bug report
    /// waiting to happen. What this buys is the middle ground - a green that leans toward the brand
    /// without stopping being green.
    /// </para>
    /// <para>
    /// It is off by default for two reasons. It is the only knob that can make the result disagree
    /// with the packaged palettes when the seed IS their primary, which costs the "seed blue, get
    /// Fluent back" property. And Material's own 15° cap is measured in CAM16 hue, not OKLCH: ported
    /// across unchanged it over-rotates badly here - a blue seed pulls the packaged green to a teal
    /// and the red to a magenta. Around <c>6</c>-<c>8</c> is the useful range in this space; treat
    /// anything higher as a stylistic choice to look at rather than a default to trust.
    /// </para>
    /// </remarks>
    public double SemanticHarmonizationDegrees { get; set; }

    /// <summary>
    /// Places the secondary accent at an absolute number of degrees from the seed's hue, replacing the
    /// packaged palette's own spacing. <see langword="null"/> (the default) keeps that spacing - about
    /// 157°, a near-complement, which is what makes the pairing read as a deliberate second accent
    /// rather than a shade of the first - by rotating secondary with primary rather than pinning it.
    /// </summary>
    public double? SecondaryHueShift { get; set; }

    /// <summary>
    /// Whether the neutral families - backgrounds, foregrounds, borders, and the gray ramp - are
    /// generated at all. <see langword="false"/> restricts the result to the accent and status roles,
    /// leaving every surface and text color to the packaged stylesheet, which is the right choice when
    /// the app has its own surface design and only wants its brand color threaded through the
    /// components.
    /// </summary>
    public bool IncludeNeutrals { get; set; } = true;

    internal static BitThemeSeedOptions Resolve(BitThemeSeedOptions? options) => options ?? new BitThemeSeedOptions();
}
