using System;

namespace Bit.BlazorUI;

/// <summary>
/// Builds a COMPLETE theme - accents, status roles, surfaces, text, strokes and the gray ramp - from
/// one seed color, for <see cref="BitThemeFactory.CreateLightThemeFromSeed(string, BitThemeSeedOptions?)"/>.
/// </summary>
/// <remarks>
/// <para>
/// The palette is not derived from step constants; it is the packaged Fluent palette
/// (<see cref="BitThemeFluentPalettes"/>) with a hue rotation applied. Those palettes are hand-solved
/// against the WCAG floors their own stylesheets document, and no formula reproduces hand-solved
/// values - the ramps carry deliberate irregularities (a compressed dark tier where the on-color is
/// black, a light tier that stops at white) that a fitted curve smooths away. Transforming them
/// instead means seeding the packaged primary returns the packaged palette byte for byte, and every
/// other seed inherits relationships that were solved by hand rather than approximated.
/// </para>
/// <para>
/// What the seed changes, by family:
/// </para>
/// <list type="bullet">
/// <item><b>Primary</b> - rotated onto the seed's hue, and shifted in lightness and chroma so its main
/// slot lands exactly on the brand color the caller passed.</item>
/// <item><b>Secondary, tertiary, info</b> - rotated by the same angle, which preserves the packaged
/// palette's own relationships between them (the ~157° between primary and secondary, info sitting on
/// primary's hue) without those numbers having to be written down anywhere.</item>
/// <item><b>Status roles</b> - untouched, so green stays green and red stays red, unless
/// <see cref="BitThemeSeedOptions.SemanticHarmonizationDegrees"/> asks them to lean toward the brand.</item>
/// <item><b>Neutrals</b> - rotated too, which tints the dark palette's already-tinted surfaces onto
/// the new hue. The light palette's neutrals are pure gray, so rotation alone leaves them gray;
/// <see cref="BitThemeSeedOptions.NeutralTintChroma"/> is what adds a tint there.</item>
/// </list>
/// <para>
/// Rotating hue at constant OKLCH lightness does move WCAG relative luminance a little, so a
/// contrast-repair pass runs afterwards and pulls back any ramp step whose on-color would no longer
/// clear the floor. On the packaged seed nothing violates and the pass is a no-op, which is what
/// keeps the reproduction exact.
/// </para>
/// </remarks>
internal static class BitThemeSeedPalette
{
    // The on-color is body text on its fill (WCAG 1.4.3 AA). Disabled is exempt from 1.4.3, but
    // unreadable is still unreadable - the packaged palettes hold the pair at 3:1 and so does this.
    private const double OnColorFloor = 4.5;
    private const double DisabledFloor = 3.0;

    // Chroma below this is not a hue, it is rounding: the packaged light neutrals are pure gray, and
    // their measured "hue" is whatever the conversion happened to produce.
    private const double AchromaticChroma = 1e-4;

    /// <summary>
    /// How a family follows the seed. Only the primary family takes the lightness and chroma deltas -
    /// everywhere else the packaged tone is the designed one and only its hue travels.
    /// </summary>
    private readonly record struct FamilyShift(double Hue, double AnchorLightness, double Lightness, double ChromaScale)
    {
        internal static FamilyShift Rotate(double hue) => new(hue, 0, 0, 1);
    }

    internal static BitTheme Generate(string seedHex, BitThemeColorScheme scheme, BitThemeSeedOptions options)
    {
        var isDark = scheme is BitThemeColorScheme.Dark;

        var (seedL, seedC, seedH) = Oklch(seedHex);

        // The shift is always measured against the LIGHT primary, because that is the slot a brand
        // color is quoted for, and then applied to whichever palette is being generated. That is what
        // makes the dark scheme inherit the packaged light-to-dark relationship (#1276C6 → #4FA3F4)
        // instead of re-deriving it: brightening the seed and comparing it to the packaged dark
        // primary lands a few units off, which would leak a small error into every family.
        var (baseL, baseC, baseH) = Oklch(BitThemeFluentPalettes.LightPri[0]);

        var accentShift = new FamilyShift(
            Hue: SignedHueDelta(baseH, seedH),
            AnchorLightness: baseL,
            Lightness: seedL - baseL,
            ChromaScale: baseC > AchromaticChroma ? seedC / baseC : 0);

        var targetH = seedH;

        // Everything but primary keeps its designed weight and only turns with the brand.
        var rotation = FamilyShift.Rotate(accentShift.Hue);

        var harmonization = Math.Max(options.SemanticHarmonizationDegrees, 0);

        var theme = new BitTheme();

        FillRole(theme.Color.Primary, Role(isDark, RoleKey.Primary), accentShift);

        // A caller-set shift replaces the packaged relationship rather than adding to it, so the
        // number means the same thing whichever palette it is measured against.
        var secondaryShift = options.SecondaryHueShift is { } absolute
            ? FamilyShift.Rotate(SignedHueDelta(Oklch(Role(isDark, RoleKey.Secondary)[0]).H, NormalizeDegrees(targetH + absolute)))
            : rotation;

        FillRole(theme.Color.Secondary, Role(isDark, RoleKey.Secondary), secondaryShift);
        FillRole(theme.Color.Tertiary, Role(isDark, RoleKey.Tertiary), rotation);
        FillRole(theme.Color.Info, Role(isDark, RoleKey.Info), rotation);

        FillRole(theme.Color.Success, Role(isDark, RoleKey.Success), StatusShift(Role(isDark, RoleKey.Success)[0], targetH, harmonization));
        FillRole(theme.Color.Warning, Role(isDark, RoleKey.Warning), StatusShift(Role(isDark, RoleKey.Warning)[0], targetH, harmonization));
        FillRole(theme.Color.SevereWarning, Role(isDark, RoleKey.SevereWarning), StatusShift(Role(isDark, RoleKey.SevereWarning)[0], targetH, harmonization));
        FillRole(theme.Color.Error, Role(isDark, RoleKey.Error), StatusShift(Role(isDark, RoleKey.Error)[0], targetH, harmonization));

        var requiredHex = isDark ? BitThemeFluentPalettes.DarkRequired : BitThemeFluentPalettes.LightRequired;
        theme.Color.Required = Shift(requiredHex, StatusShift(requiredHex, targetH, harmonization), shiftLightness: false);

        if (options.IncludeNeutrals)
        {
            FillNeutrals(theme, isDark, rotation, targetH, Math.Max(options.NeutralTintChroma ?? 0, 0));
        }

        return theme;
    }

    private enum RoleKey { Primary, Secondary, Tertiary, Info, Success, Warning, SevereWarning, Error }

    private static string[] Role(bool isDark, RoleKey key) => (isDark, key) switch
    {
        (false, RoleKey.Primary) => BitThemeFluentPalettes.LightPri,
        (false, RoleKey.Secondary) => BitThemeFluentPalettes.LightSec,
        (false, RoleKey.Tertiary) => BitThemeFluentPalettes.LightTer,
        (false, RoleKey.Info) => BitThemeFluentPalettes.LightInf,
        (false, RoleKey.Success) => BitThemeFluentPalettes.LightSuc,
        (false, RoleKey.Warning) => BitThemeFluentPalettes.LightWrn,
        (false, RoleKey.SevereWarning) => BitThemeFluentPalettes.LightSwr,
        (false, RoleKey.Error) => BitThemeFluentPalettes.LightErr,
        (true, RoleKey.Primary) => BitThemeFluentPalettes.DarkPri,
        (true, RoleKey.Secondary) => BitThemeFluentPalettes.DarkSec,
        (true, RoleKey.Tertiary) => BitThemeFluentPalettes.DarkTer,
        (true, RoleKey.Info) => BitThemeFluentPalettes.DarkInf,
        (true, RoleKey.Success) => BitThemeFluentPalettes.DarkSuc,
        (true, RoleKey.Warning) => BitThemeFluentPalettes.DarkWrn,
        (true, RoleKey.SevereWarning) => BitThemeFluentPalettes.DarkSwr,
        _ => BitThemeFluentPalettes.DarkErr,
    };

    // Material 3's Blend.harmonize: rotate half the way toward the seed, capped, the shorter way
    // round. Half-the-distance means a hue already near the brand barely moves; the cap is what stops
    // a far-away hue from losing the identity that makes it mean "error" in the first place.
    private static FamilyShift StatusShift(string anchorHex, double seedHue, double maxDegrees)
    {
        if (maxDegrees <= 0) return FamilyShift.Rotate(0);

        var anchorHue = Oklch(anchorHex).H;
        var distance = Math.Abs(SignedHueDelta(anchorHue, seedHue));
        var rotation = Math.Min(distance * 0.5, maxDegrees);

        return FamilyShift.Rotate(Math.Sign(SignedHueDelta(anchorHue, seedHue)) * rotation);
    }

    private static void FillRole(BitThemeColorVariants variants, string[] template, in FamilyShift shift)
    {
        variants.Main = Shift(template[0], shift, shiftLightness: true);
        variants.MainHover = Shift(template[1], shift, shiftLightness: true);
        variants.MainActive = Shift(template[2], shift, shiftLightness: true);
        variants.Dark = Shift(template[3], shift, shiftLightness: true);
        variants.DarkHover = Shift(template[4], shift, shiftLightness: true);
        variants.DarkActive = Shift(template[5], shift, shiftLightness: true);
        variants.Light = Shift(template[6], shift, shiftLightness: true);
        variants.LightHover = Shift(template[7], shift, shiftLightness: true);
        variants.LightActive = Shift(template[8], shift, shiftLightness: true);

        // The on-color is a choice, not a tone: it stays whichever of the palette's two label colors
        // reads on the new main. Keeping the packaged one when it still works is what makes the
        // packaged seed reproduce exactly.
        variants.Text = ResolveOnColor(template[9], variants.Main!);

        // The disabled pair is an absolute weight - the state has to look the same on every role - so
        // it turns and desaturates with the brand but never takes the seed's own lightness.
        variants.Disabled = Shift(template[10], shift, shiftLightness: false);
        variants.DisabledText = Shift(template[11], shift, shiftLightness: false);

        // Focus is deliberately left unset: the packaged palettes alias --bit-clr-*-focus to the
        // role's own color with var(), so leaving it alone keeps that aliasing live instead of
        // freezing the indicator to this palette.

        RepairContrast(variants);
    }

    private static void FillNeutrals(BitTheme theme, bool isDark, in FamilyShift rotation, double seedHue, double tint)
    {
        FillTier(theme.Color.Foreground, isDark ? BitThemeFluentPalettes.DarkFgPri : BitThemeFluentPalettes.LightFgPri, NeutralTier.Primary, rotation, seedHue, tint);
        FillTier(theme.Color.Foreground, isDark ? BitThemeFluentPalettes.DarkFgSec : BitThemeFluentPalettes.LightFgSec, NeutralTier.Secondary, rotation, seedHue, tint);
        FillTier(theme.Color.Foreground, isDark ? BitThemeFluentPalettes.DarkFgTer : BitThemeFluentPalettes.LightFgTer, NeutralTier.Tertiary, rotation, seedHue, tint);
        theme.Color.Foreground.Disabled = Tint(isDark ? BitThemeFluentPalettes.DarkFgDisabled : BitThemeFluentPalettes.LightFgDisabled, rotation, seedHue, tint);

        FillTier(theme.Color.Background, isDark ? BitThemeFluentPalettes.DarkBgPri : BitThemeFluentPalettes.LightBgPri, NeutralTier.Primary, rotation, seedHue, tint);
        FillTier(theme.Color.Background, isDark ? BitThemeFluentPalettes.DarkBgSec : BitThemeFluentPalettes.LightBgSec, NeutralTier.Secondary, rotation, seedHue, tint);
        FillTier(theme.Color.Background, isDark ? BitThemeFluentPalettes.DarkBgTer : BitThemeFluentPalettes.LightBgTer, NeutralTier.Tertiary, rotation, seedHue, tint);
        theme.Color.Background.Disabled = Tint(isDark ? BitThemeFluentPalettes.DarkBgDisabled : BitThemeFluentPalettes.LightBgDisabled, rotation, seedHue, tint);
        theme.Color.Background.Overlay = isDark ? BitThemeFluentPalettes.DarkOverlay : BitThemeFluentPalettes.LightOverlay;

        FillTier(theme.Color.Border, isDark ? BitThemeFluentPalettes.DarkBrdPri : BitThemeFluentPalettes.LightBrdPri, NeutralTier.Primary, rotation, seedHue, tint);
        FillTier(theme.Color.Border, isDark ? BitThemeFluentPalettes.DarkBrdSec : BitThemeFluentPalettes.LightBrdSec, NeutralTier.Secondary, rotation, seedHue, tint);
        FillTier(theme.Color.Border, isDark ? BitThemeFluentPalettes.DarkBrdTer : BitThemeFluentPalettes.LightBrdTer, NeutralTier.Tertiary, rotation, seedHue, tint);
        theme.Color.Border.Disabled = Tint(isDark ? BitThemeFluentPalettes.DarkBrdDisabled : BitThemeFluentPalettes.LightBrdDisabled, rotation, seedHue, tint);

        // The ramp is used raw by app CSS, where a visible tint on something named "gray70" would be
        // a surprise, so it takes a fraction of what the surfaces take.
        FillNeutralRamp(theme.Color.Neutral, rotation, seedHue, tint * 0.45);
    }

    private enum NeutralTier { Primary, Secondary, Tertiary }

    private static void FillTier(
        BitThemeGeneralColorVariants target,
        string[] template,
        NeutralTier tier,
        in FamilyShift rotation,
        double seedHue,
        double tint)
    {
        var tones = new string[BitThemeFluentPalettes.TierSlots];
        for (var i = 0; i < tones.Length; i++)
        {
            tones[i] = Tint(template[i], rotation, seedHue, tint);
        }

        switch (tier)
        {
            case NeutralTier.Primary:
                (target.Primary, target.PrimaryHover, target.PrimaryActive) = (tones[0], tones[1], tones[2]);
                (target.PrimaryDark, target.PrimaryDarkHover, target.PrimaryDarkActive) = (tones[3], tones[4], tones[5]);
                (target.PrimaryLight, target.PrimaryLightHover, target.PrimaryLightActive) = (tones[6], tones[7], tones[8]);
                (target.PrimaryDisabled, target.PrimaryDisabledText) = (tones[9], tones[10]);
                break;

            case NeutralTier.Secondary:
                (target.Secondary, target.SecondaryHover, target.SecondaryActive) = (tones[0], tones[1], tones[2]);
                (target.SecondaryDark, target.SecondaryDarkHover, target.SecondaryDarkActive) = (tones[3], tones[4], tones[5]);
                (target.SecondaryLight, target.SecondaryLightHover, target.SecondaryLightActive) = (tones[6], tones[7], tones[8]);
                (target.SecondaryDisabled, target.SecondaryDisabledText) = (tones[9], tones[10]);
                break;

            default:
                (target.Tertiary, target.TertiaryHover, target.TertiaryActive) = (tones[0], tones[1], tones[2]);
                (target.TertiaryDark, target.TertiaryDarkHover, target.TertiaryDarkActive) = (tones[3], tones[4], tones[5]);
                (target.TertiaryLight, target.TertiaryLightHover, target.TertiaryLightActive) = (tones[6], tones[7], tones[8]);
                (target.TertiaryDisabled, target.TertiaryDisabledText) = (tones[9], tones[10]);
                break;
        }
    }

    private static void FillNeutralRamp(BitThemeNeutralColorVariants neutral, in FamilyShift rotation, double seedHue, double tint)
    {
        var ramp = BitThemeFluentPalettes.Neutrals;

        // White and black are the two colors a tint would only spoil.
        neutral.White = ramp[0];
        neutral.Black = ramp[1];

        neutral.Gray10 = Tint(ramp[2], rotation, seedHue, tint);
        neutral.Gray20 = Tint(ramp[3], rotation, seedHue, tint);
        neutral.Gray30 = Tint(ramp[4], rotation, seedHue, tint);
        neutral.Gray40 = Tint(ramp[5], rotation, seedHue, tint);
        neutral.Gray50 = Tint(ramp[6], rotation, seedHue, tint);
        neutral.Gray60 = Tint(ramp[7], rotation, seedHue, tint);
        neutral.Gray70 = Tint(ramp[8], rotation, seedHue, tint);
        neutral.Gray80 = Tint(ramp[9], rotation, seedHue, tint);
        neutral.Gray90 = Tint(ramp[10], rotation, seedHue, tint);
        neutral.Gray100 = Tint(ramp[11], rotation, seedHue, tint);
        neutral.Gray110 = Tint(ramp[12], rotation, seedHue, tint);
        neutral.Gray120 = Tint(ramp[13], rotation, seedHue, tint);
        neutral.Gray130 = Tint(ramp[14], rotation, seedHue, tint);
        neutral.Gray140 = Tint(ramp[15], rotation, seedHue, tint);
        neutral.Gray150 = Tint(ramp[16], rotation, seedHue, tint);
        neutral.Gray160 = Tint(ramp[17], rotation, seedHue, tint);
        neutral.Gray170 = Tint(ramp[18], rotation, seedHue, tint);
        neutral.Gray180 = Tint(ramp[19], rotation, seedHue, tint);
        neutral.Gray190 = Tint(ramp[20], rotation, seedHue, tint);
        neutral.Gray200 = Tint(ramp[21], rotation, seedHue, tint);
        neutral.Gray210 = Tint(ramp[22], rotation, seedHue, tint);
        neutral.Gray220 = Tint(ramp[23], rotation, seedHue, tint);
    }

    // ── Tone transforms ───────────────────────────────────────────────────────────────────────

    private static string Shift(string hex, in FamilyShift shift, bool shiftLightness)
    {
        var (l, c, h) = Oklch(hex);

        // An achromatic tone has no hue to rotate and no chroma to scale; rotating one would only
        // introduce a color the packaged palette deliberately left out.
        if (c <= AchromaticChroma) return shiftLightness && shift.Lightness != 0 ? ToHex(RemapLightness(l, shift), c, h) : hex;

        return ToHex(
            shiftLightness ? RemapLightness(l, shift) : l,
            c * shift.ChromaScale,
            h + shift.Hue);
    }

    /// <summary>
    /// Moves a template tone's lightness onto the seed's as a remap rather than an addition: the
    /// template primary's own lightness is pinned to the seed's, and the room left on either side of
    /// it - down to black, up to white - is stretched or squeezed to fit. Both endpoints stay put and
    /// the curve is strictly increasing, so a ramp keeps its order and its steps stay distinct.
    /// </summary>
    /// <remarks>
    /// Adding the delta instead runs the far end of a ramp off the scale, where <c>ToRgb</c> clamps
    /// it: seeding a light brand color (a marigold, a cyan) collapsed <c>light</c>,
    /// <c>light-hover</c> and <c>light-active</c> onto one identical white in the light palette, and
    /// <c>main</c>, <c>main-hover</c> and <c>main-active</c> in the dark one - three interactive
    /// states rendering as the same color. The remap cannot produce that. It is the identity when the
    /// seed sits at the template primary's own lightness, so seeding a packaged palette's primary
    /// still reproduces that palette byte for byte.
    /// </remarks>
    private static double RemapLightness(double l, in FamilyShift shift)
    {
        if (shift.Lightness == 0) return l;

        var anchor = shift.AnchorLightness;
        var target = Math.Clamp(anchor + shift.Lightness, 0.0, 1.0);

        // A template primary at pure black or pure white leaves no room on one side to scale, so
        // there is no ramp to preserve: the whole family lands on the seed's own lightness.
        if (anchor <= 0.0 || anchor >= 1.0) return target;

        return l <= anchor
            ? target * (l / anchor)
            : 1.0 - ((1.0 - target) * ((1.0 - l) / (1.0 - anchor)));
    }

    /// <summary>
    /// A neutral tone: it turns with the brand and optionally picks up a tint. A gray has no hue of
    /// its own, so the tint is applied at the seed's hue rather than at whatever the conversion
    /// reported for a zero-chroma color.
    /// </summary>
    private static string Tint(string hex, in FamilyShift rotation, double seedHue, double tint)
    {
        if (tint <= 0 && rotation.Hue == 0) return hex;

        var (l, c, h) = Oklch(hex);
        if (tint <= 0 && c <= AchromaticChroma) return hex;

        return ToHex(l, c + tint, c > AchromaticChroma ? h + rotation.Hue : seedHue);
    }

    /// <summary>
    /// Keeps the palette's own on-color when it still reads on the new main, and otherwise swaps to
    /// the other one the palettes use - so a flipped role still looks like it came from the same
    /// design rather than reaching for raw black.
    /// </summary>
    /// <remarks>
    /// The palettes' dark label is <c>#141414</c>, not black, which costs about a fifth of a point of
    /// contrast. That is free almost everywhere, but there is a narrow band of mid-luminance mains
    /// where black and white cross at 4.58:1 and neither label quite clears the floor. A main cannot
    /// be moved to fix it - on the primary role it IS the brand color the caller asked for - so the
    /// last resort is the pole itself, which always clears.
    /// </remarks>
    private static string ResolveOnColor(string packagedOnColor, string mainHex)
    {
        if (BitThemeColorContrast.GetContrastRatio(packagedOnColor, mainHex) >= OnColorFloor) return packagedOnColor;

        var candidate = Better("#141414", "#FFFFFF");

        return BitThemeColorContrast.GetContrastRatio(candidate, mainHex) >= OnColorFloor
            ? candidate
            : Better("#000000", "#FFFFFF");

        string Better(string a, string b)
            => BitThemeColorContrast.GetContrastRatio(a, mainHex) >= BitThemeColorContrast.GetContrastRatio(b, mainHex) ? a : b;
    }

    // ── Contrast repair ───────────────────────────────────────────────────────────────────────
    // Hue rotation happens at constant OKLCH lightness, which is perceptual - WCAG contrast is not,
    // so a rotated ramp can drift below the floor its packaged original cleared. This pulls the
    // offending tier back toward main until it clears again, which is the same shallower ramp the
    // packaged palettes give the roles that need one. On the packaged seed nothing violates and the
    // whole pass is a no-op, which is what keeps that reproduction exact.

    private static void RepairContrast(BitThemeColorVariants variants)
    {
        if (variants.Text is null || variants.Main is null) return;

        CompressTier(variants.Main, variants.Text,
            () => [variants.MainHover, variants.MainActive],
            values => (variants.MainHover, variants.MainActive) = (values[0], values[1]));

        CompressTier(variants.Main, variants.Text,
            () => [variants.Dark, variants.DarkHover, variants.DarkActive],
            values => (variants.Dark, variants.DarkHover, variants.DarkActive) = (values[0], values[1], values[2]));

        if (variants.Disabled is not null && variants.DisabledText is not null)
        {
            variants.DisabledText = PushApart(variants.Disabled, variants.DisabledText, DisabledFloor);
        }
    }

    /// <summary>
    /// Pulls a whole tier back toward <paramref name="mainHex"/> by one shared factor until every
    /// step in it clears <see cref="OnColorFloor"/> against the on-color. One factor for the tier
    /// rather than one per step keeps the steps ordered and evenly spaced - repairing them
    /// individually would let a deeper step overtake a shallower one.
    /// </summary>
    /// <remarks>
    /// The search always succeeds. The on-color is one that reads on main (that is how it was
    /// resolved), and contrast against a fixed on-color changes monotonically as a step travels back
    /// toward main, so factor 0 always clears and brackets the search. That is why the travel is in
    /// all three OKLCH coordinates rather than lightness alone: holding a step's own chroma and hue
    /// leaves factor 0 a color that is main's lightness but NOT main, which can sit a hundredth
    /// below the floor when main itself only just clears it - and the search then has no bracket and
    /// gives up with the tier unrepaired.
    /// </remarks>
    private static void CompressTier(string mainHex, string textHex, Func<string?[]> get, Action<string[]> set)
    {
        var fills = get();
        if (Array.Exists(fills, f => f is null)) return;

        var values = Array.ConvertAll(fills, f => f!);
        if (Array.TrueForAll(values, f => BitThemeColorContrast.GetContrastRatio(f, textHex) >= OnColorFloor)) return;

        var (mainL, mainC, mainH) = Oklch(mainHex);
        var steps = Array.ConvertAll(values, Oklch);

        string toward((double L, double C, double H) step, double factor)
            => ToHex(
                mainL + ((step.L - mainL) * factor),
                mainC + ((step.C - mainC) * factor),
                mainH + (SignedHueDelta(mainH, step.H) * factor));

        bool clears(double factor)
        {
            foreach (var step in steps)
            {
                if (BitThemeColorContrast.GetContrastRatio(toward(step, factor), textHex) < OnColorFloor) return false;
            }

            return true;
        }

        // Largest surviving factor, so the ramp keeps as much of its designed depth as the floor allows.
        double low = 0.0, high = 1.0;
        for (var i = 0; i < 16; i++)
        {
            var mid = (low + high) / 2.0;
            if (clears(mid)) low = mid; else high = mid;
        }

        set(Array.ConvertAll(steps, step => toward(step, low)));
    }

    /// <summary>
    /// Moves a label away from the fill it sits on, toward whichever pole it is already heading for,
    /// until the pair clears <paramref name="floor"/>. Used for the disabled pair, whose two tones are
    /// both absolute weights and can converge once they turn onto a new hue.
    /// </summary>
    private static string PushApart(string fillHex, string textHex, double floor)
    {
        if (BitThemeColorContrast.GetContrastRatio(fillHex, textHex) >= floor) return textHex;

        var (fillL, _, _) = Oklch(fillHex);
        var (textL, textC, textH) = Oklch(textHex);

        // A pole always clears the floor against any fill, so pushing far enough always works.
        var pole = fillL < 0.5 ? 1.0 : 0.0;

        double low = 0.0, high = 1.0;
        for (var i = 0; i < 16; i++)
        {
            var mid = (low + high) / 2.0;
            if (BitThemeColorContrast.GetContrastRatio(fillHex, ToHex(textL + ((pole - textL) * mid), textC, textH)) >= floor) high = mid; else low = mid;
        }

        return ToHex(textL + ((pole - textL) * high), textC, textH);
    }

    // ── Color helpers ─────────────────────────────────────────────────────────────────────────

    private static (double L, double C, double H) Oklch(string hex)
    {
        var color = new BitInternalColor(hex);
        return BitThemeOklch.FromRgb(color.R, color.G, color.B);
    }

    private static string ToHex(double l, double c, double h)
        => BitThemeOklch.ToHex(Math.Clamp(l, 0.0, 1.0), Math.Max(c, 0.0), NormalizeDegrees(h));

    /// <summary>The shortest signed rotation from <paramref name="from"/> to <paramref name="to"/>, in (-180, 180].</summary>
    private static double SignedHueDelta(double from, double to)
    {
        var delta = NormalizeDegrees(to - from);
        return delta > 180.0 ? delta - 360.0 : delta;
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
