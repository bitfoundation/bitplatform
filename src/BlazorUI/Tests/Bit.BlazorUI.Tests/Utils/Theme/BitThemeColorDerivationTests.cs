using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeColorDerivationTests
{
    // ── Guard clauses ──────────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainNullVariantsDoesNotThrow()
    {
        // Should return silently – no exception expected.
        BitThemeColorDerivation.FillColorRoleFromMain(null!, "#FF0000");
    }

    [TestMethod]
    public void FillColorRoleFromMainNullHexDoesNotThrow()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, null!);
        Assert.IsNull(v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMainEmptyHexDoesNotThrow()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "");
        Assert.IsNull(v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMainInvalidHexIsNoOp()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "not-a-color");

        // An unrecognized hex is now treated as "nothing to derive from" (a no-op), consistent
        // with the null/empty guards and with BitThemeColorContrast's validation. The previous
        // behavior fabricated an all-white palette, which masked the mistake and overrode
        // stylesheet defaults; the variants must stay untouched instead.
        Assert.IsNull(v.Main,        "Main");
        Assert.IsNull(v.Dark,        "Dark");
        Assert.IsNull(v.Light,       "Light");
        Assert.IsNull(v.Text,        "Text");
        Assert.IsNull(v.MainHover,   "MainHover");
        Assert.IsNull(v.MainActive,  "MainActive");
        Assert.IsNull(v.DarkHover,   "DarkHover");
        Assert.IsNull(v.DarkActive,  "DarkActive");
        Assert.IsNull(v.LightHover,  "LightHover");
        Assert.IsNull(v.LightActive, "LightActive");
        Assert.IsNull(v.Disabled,     "Disabled");
        Assert.IsNull(v.DisabledText, "DisabledText");
        Assert.IsNull(v.Focus,        "Focus");
    }

    [TestMethod]
    public void FillColorRoleFromMainShorthandHexExpandsLikeSixDigit()
    {
        // '#FFF' must be expanded to '#FFFFFF' (matching BitThemeColorContrast), not misparsed by
        // the underlying 24-bit parser. The derived Main should equal the six-digit equivalent.
        var shorthand = new BitThemeColorVariants();
        var full = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(shorthand, "#FFF");
        BitThemeColorDerivation.FillColorRoleFromMain(full, "#FFFFFF");

        Assert.IsNotNull(shorthand.Main);
        Assert.AreEqual(full.Main, shorthand.Main);
    }

    // ── All slots populated ────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainValidColorAllVariantsPopulated()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");

        Assert.IsNotNull(v.Main,        "Main");
        Assert.IsNotNull(v.MainHover,   "MainHover");
        Assert.IsNotNull(v.MainActive,  "MainActive");
        Assert.IsNotNull(v.Dark,        "Dark");
        Assert.IsNotNull(v.DarkHover,   "DarkHover");
        Assert.IsNotNull(v.DarkActive,  "DarkActive");
        Assert.IsNotNull(v.Light,       "Light");
        Assert.IsNotNull(v.LightHover,  "LightHover");
        Assert.IsNotNull(v.LightActive, "LightActive");
        Assert.IsNotNull(v.Text,        "Text");
        Assert.IsNotNull(v.Disabled,     "Disabled");
        Assert.IsNotNull(v.DisabledText, "DisabledText");
        Assert.IsNotNull(v.Focus,        "Focus");
    }

    // ── Pre-set values are never overwritten ───────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainPresetMainNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Main = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetLightNotOverwritten()
    {
        const string preset = "#FFFFFF";
        var v = new BitThemeColorVariants { Light = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");
        Assert.AreEqual(preset, v.Light);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetMainHoverNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { MainHover = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.MainHover);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetMainActiveNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { MainActive = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.MainActive);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetDarkNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Dark = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Dark);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetDarkHoverNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { DarkHover = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.DarkHover);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetDarkActiveNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { DarkActive = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.DarkActive);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetLightHoverNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { LightHover = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.LightHover);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetLightActiveNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { LightActive = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.LightActive);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetTextNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Text = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetDisabledNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Disabled = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Disabled);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetDisabledTextNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { DisabledText = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.DisabledText);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetFocusNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Focus = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Focus);
    }

    // ── Focus aliases the resolved Main ───────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainFocusAliasesResolvedMain()
    {
        // The packaged palettes set --bit-clr-<role>-focus to the role's main color; the derived
        // Focus mirrors that aliasing.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");
        Assert.AreEqual(v.Main, v.Focus);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetNonHexMainFocusCopiesIt()
    {
        // A non-hex preset Main (e.g. a CSS var() reference) can't be color-shifted, but the
        // Focus-aliases-Main convention still holds by copying the string verbatim.
        var v = new BitThemeColorVariants { Main = "var(--brand-main)" };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0");
        Assert.AreEqual("var(--brand-main)", v.Focus);
    }

    // ── Hex format ────────────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainValidColorHexValuesStartWithHash()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");

        foreach (var (name, value) in new[]
        {
            ("Main",        v.Main),
            ("MainHover",   v.MainHover),
            ("MainActive",  v.MainActive),
            ("Dark",        v.Dark),
            ("DarkHover",   v.DarkHover),
            ("DarkActive",  v.DarkActive),
            ("Light",       v.Light),
            ("LightHover",  v.LightHover),
            ("LightActive", v.LightActive),
            ("Text",        v.Text),
        })
        {
            Assert.IsTrue(value!.StartsWith('#'), $"{name} should start with '#' but was '{value}'");
        }
    }

    // ── Dark variants are darker than Main ────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainDarkVariantsAreDarkerThanMain()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");

        var mainLum   = Luminance(v.Main!);
        var darkLum   = Luminance(v.Dark!);
        var dHoverLum = Luminance(v.DarkHover!);
        var dActLum   = Luminance(v.DarkActive!);

        Assert.IsTrue(darkLum   < mainLum, "Dark should be darker than Main");
        Assert.IsTrue(dHoverLum < darkLum, "DarkHover should be darker than Dark");
        Assert.IsTrue(dActLum   < dHoverLum, "DarkActive should be darker than DarkHover");
    }

    // ── Light variants are lighter than Main ──────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainLightVariantsAreLighterThanMain()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");

        var mainLum      = Luminance(v.Main!);
        var lightLum     = Luminance(v.Light!);
        var lHoverLum    = Luminance(v.LightHover!);
        var lActiveLum   = Luminance(v.LightActive!);

        // The light family is a tinted background: Light is the palest step and its hover/active
        // states step BACK toward Main (a tinted surface darkens on interaction) - matching the
        // packaged Fluent palette (pri-light #A3CFEF → hover #8CC3EC → active #76B6E8). All three
        // stay lighter than Main.
        Assert.IsTrue(lightLum   > mainLum,   "Light should be lighter than Main");
        Assert.IsTrue(lHoverLum  > mainLum,   "LightHover should be lighter than Main");
        Assert.IsTrue(lActiveLum > mainLum,   "LightActive should be lighter than Main");
        Assert.IsTrue(lightLum   > lHoverLum,  "Light should be lighter than LightHover");
        Assert.IsTrue(lHoverLum  > lActiveLum, "LightHover should be lighter than LightActive");
    }

    // ── Light steps are distinct even for high-brightness colors ──────────────

    [TestMethod]
    public void FillColorRoleFromMainHighBrightnessColorLightStepsAreDistinct()
    {
        // Near-white seeds are where lightening math can collapse to pure white; the OKLab
        // white-mix interpolates toward 1 without ever reaching it, so steps stay distinguishable.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#E8E8E8"); // near-white grey

        // All three light variants must differ from Main.
        Assert.AreNotEqual(v.Main, v.Light,       "Light must differ from Main for high-v color");
        Assert.AreNotEqual(v.Main, v.LightHover,  "LightHover must differ from Main for high-v color");
        Assert.AreNotEqual(v.Main, v.LightActive, "LightActive must differ from Main for high-v color");
    }

    [TestMethod]
    public void FillColorRoleFromMainHighBrightnessColorLightStepsMutuallyDistinct()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#B0C8E0"); // mid-high brightness

        Assert.AreNotEqual(v.Light,      v.LightHover,  "Light and LightHover must differ");
        Assert.AreNotEqual(v.LightHover, v.LightActive, "LightHover and LightActive must differ");
        Assert.AreNotEqual(v.Light,      v.LightActive, "Light and LightActive must differ");
    }

    [TestMethod]
    public void FillColorRoleFromMainVeryHighBrightnessColorLightStepsMutuallyDistinct()
    {
        // The white-mix fractions (0.58/0.475/0.375) interpolate toward white instead of adding a
        // fixed offset, so even a bright seed keeps three distinct tint steps - nothing clamps to
        // pure white below an exactly-white seed.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#D0D0D0");

        Assert.AreNotEqual(v.Light,      v.LightHover,  "Light and LightHover must differ at high brightness");
        Assert.AreNotEqual(v.LightHover, v.LightActive, "LightHover and LightActive must differ at high brightness");
    }

    // ── Text contrast suggestion ───────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainDarkBaseColorTextIsWhite()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#1A1A2E");
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainLightBaseColorTextIsBlack()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0");
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainMidGrayTextIsBlack()
    {
        // #888888: white contrast ≈3.55 vs black ≈5.92 → black is the higher-contrast on-color text.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#888888");
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainSaturatedColorTextIsBlack()
    {
        // #009100 reads dark by naive YIQ brightness (≈0.334, which would suggest white), but its WCAG
        // relative luminance is ≈0.20: white contrast ≈4.16 vs black ≈5.05. The contrast-based choice
        // correctly keeps black as the higher-contrast (and WCAG-AA-optimal) option.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#009100");
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainMidGrayTieBreakPicksHigherContrast()
    {
        // #757575: both candidates pass WCAG AA (black ≈4.558, white ≈4.607). The higher-contrast
        // option (white) is chosen.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#757575");
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetHexMainDrivesTextSuggestion()
    {
        // Main preset to a concrete hex different from mainHex: Text contrast is evaluated against
        // the preset Main (near-black → white), not mainHex (near-white, which would pick black).
        var v = new BitThemeColorVariants { Main = "#101010" };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0");
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainPresetNonHexMainLeavesTextUnset()
    {
        // Main preset to a CSS variable: the actual background color cannot be evaluated, so Text
        // stays unset for the stylesheet/theme default to apply instead of guessing against mainHex.
        var v = new BitThemeColorVariants { Main = "var(--brand-main)" };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0");
        Assert.IsNull(v.Text);
    }

    // ── Perceptual (OKLCH) guarantees ─────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainVariantsAreOrderedByPerceptualLightness()
    {
        // The full family must be strictly ordered in OKLab lightness:
        // Light > LightHover > LightActive > Main > MainHover > MainActive > Dark > DarkHover > DarkActive.
        // #FFE500 (bright saturated yellow) is included deliberately: it is the classic failure
        // mode of HSV-value stepping, where V barely tracks perceived lightness.
        foreach (var seed in new[] { "#1A86D8", "#FFE500", "#6B21A8", "#107C10", "#C50F1F" })
        {
            var v = new BitThemeColorVariants();
            BitThemeColorDerivation.FillColorRoleFromMain(v, seed);

            var ordered = new[]
            {
                ("Light",       v.Light!),
                ("LightHover",  v.LightHover!),
                ("LightActive", v.LightActive!),
                ("Main",        v.Main!),
                ("MainHover",   v.MainHover!),
                ("MainActive",  v.MainActive!),
                ("Dark",        v.Dark!),
                ("DarkHover",   v.DarkHover!),
                ("DarkActive",  v.DarkActive!),
            };

            for (var i = 1; i < ordered.Length; i++)
            {
                var (lighterName, lighterHex) = ordered[i - 1];
                var (darkerName, darkerHex) = ordered[i];
                Assert.IsTrue(
                    OklabLightness(lighterHex) > OklabLightness(darkerHex),
                    $"{lighterName} ({lighterHex}) must be perceptually lighter than {darkerName} ({darkerHex}) for seed {seed}");
            }
        }
    }

    [TestMethod]
    public void FillColorRoleFromMainVariantsPreserveHue()
    {
        // Lightness shifts happen at constant OKLCH hue, so a derived family never drifts toward a
        // neighboring hue the way HSL/HSV lighten/darken can. Tolerance covers 8-bit rounding.
        foreach (var seed in new[] { "#1A86D8", "#C50F1F", "#107C10", "#6B21A8" })
        {
            var v = new BitThemeColorVariants();
            BitThemeColorDerivation.FillColorRoleFromMain(v, seed);

            var seedColor = new BitInternalColor(seed);
            var (_, _, seedHue) = BitThemeOklch.FromRgb(seedColor.R, seedColor.G, seedColor.B);

            foreach (var (name, hex) in new[]
            {
                ("MainHover",   v.MainHover!),
                ("MainActive",  v.MainActive!),
                ("Dark",        v.Dark!),
                ("DarkActive",  v.DarkActive!),
                ("Light",       v.Light!),
                ("LightActive", v.LightActive!),
                ("Disabled",     v.Disabled!),
            })
            {
                var derived = new BitInternalColor(hex);
                var (_, chroma, hue) = BitThemeOklch.FromRgb(derived.R, derived.G, derived.B);

                // Hue is numerically meaningless for near-achromatic colors.
                if (chroma < 0.02) continue;

                var delta = Math.Abs(hue - seedHue);
                if (delta > 180) delta = 360 - delta;
                Assert.IsTrue(delta < 4.0, $"{name} ({hex}) hue drifted {delta:F1}° from seed {seed}");
            }
        }
    }

    // ── Contrast floor for derived interactive states ─────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainDerivedInteractiveStatesMeetUiContrastFloor()
    {
        // The same 3.0:1 floor BitThemePaletteContrastTests enforces for the packaged palettes
        // (WCAG 1.4.11 non-text contrast), extended to DERIVED roles: for every seed on a 6-step
        // RGB grid (216 colors), the auto-selected Text must clear the floor not just against Main
        // but against the derived MainHover and MainActive fills too. This pins the derivation
        // step sizes - an over-aggressive hover/active shift would break the guarantee for seeds
        // whose luminance sits near the black/white text decision boundary.
        const double floor = 3.0;

        for (var r = 0; r <= 0xFF; r += 0x33)
        for (var g = 0; g <= 0xFF; g += 0x33)
        for (var b = 0; b <= 0xFF; b += 0x33)
        {
            var seed = FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
            var v = new BitThemeColorVariants();
            BitThemeColorDerivation.FillColorRoleFromMain(v, seed);

            foreach (var (name, hex) in new[] { ("Main", v.Main!), ("MainHover", v.MainHover!), ("MainActive", v.MainActive!) })
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(v.Text!, hex);
                Assert.IsTrue(
                    ratio >= floor,
                    $"Text {v.Text} on {name} {hex} (seed {seed}) is {ratio:F2}:1, below the {floor}:1 floor");
            }
        }
    }

    // ── Dark scheme ───────────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainDarkSchemeAllVariantsPopulated()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#569FFF", BitThemeColorScheme.Dark);

        foreach (var (name, value) in new[]
        {
            ("Main", v.Main), ("MainHover", v.MainHover), ("MainActive", v.MainActive),
            ("Dark", v.Dark), ("DarkHover", v.DarkHover), ("DarkActive", v.DarkActive),
            ("Light", v.Light), ("LightHover", v.LightHover), ("LightActive", v.LightActive),
            ("Text", v.Text), ("Disabled", v.Disabled), ("DisabledText", v.DisabledText), ("Focus", v.Focus),
        })
        {
            Assert.IsNotNull(value, name);
        }
    }

    [TestMethod]
    public void FillColorRoleFromMainDarkSchemeInvalidHexIsNoOp()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "not-a-color", BitThemeColorScheme.Dark);
        Assert.IsNull(v.Main);
        Assert.IsNull(v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainDarkSchemeVariantsAreOrderedByPerceptualLightness()
    {
        // Dark-scheme ordering differs from light in one respect: the dark family interleaves with
        // the interactive states (in the packaged dark palette, dark sits between hover and
        // active), so the chains are asserted separately: interactive states dim from Main, the
        // dark family dims further among itself, and the light family still sits above Main with
        // Light as its palest step.
        foreach (var seed in new[] { "#569FFF", "#FF9E67", "#FFE500", "#6B21A8" })
        {
            var v = new BitThemeColorVariants();
            BitThemeColorDerivation.FillColorRoleFromMain(v, seed, BitThemeColorScheme.Dark);

            var main = OklabLightness(v.Main!);

            Assert.IsTrue(OklabLightness(v.Light!) > OklabLightness(v.LightHover!), $"Light > LightHover for {seed}");
            Assert.IsTrue(OklabLightness(v.LightHover!) > OklabLightness(v.LightActive!), $"LightHover > LightActive for {seed}");
            Assert.IsTrue(OklabLightness(v.LightActive!) > main, $"LightActive > Main for {seed}");
            Assert.IsTrue(main > OklabLightness(v.MainHover!), $"Main > MainHover for {seed}");
            Assert.IsTrue(OklabLightness(v.MainHover!) > OklabLightness(v.MainActive!), $"MainHover > MainActive for {seed}");
            Assert.IsTrue(main > OklabLightness(v.Dark!), $"Main > Dark for {seed}");
            Assert.IsTrue(OklabLightness(v.Dark!) > OklabLightness(v.DarkHover!), $"Dark > DarkHover for {seed}");
            Assert.IsTrue(OklabLightness(v.DarkHover!) > OklabLightness(v.DarkActive!), $"DarkHover > DarkActive for {seed}");
            Assert.IsTrue(OklabLightness(v.Disabled!) < main, $"Disabled < Main for {seed}");
            Assert.IsTrue(OklabLightness(v.DisabledText!) > OklabLightness(v.Disabled!), $"DisabledText > Disabled for {seed}");
        }
    }

    [TestMethod]
    public void FillColorRoleFromMainDarkSchemeDerivedInteractiveStatesMeetUiContrastFloor()
    {
        // Same 3.0:1 floor as the light-scheme grid test, for the dark-scheme step directions.
        const double floor = 3.0;

        for (var r = 0; r <= 0xFF; r += 0x33)
        for (var g = 0; g <= 0xFF; g += 0x33)
        for (var b = 0; b <= 0xFF; b += 0x33)
        {
            var seed = FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
            var v = new BitThemeColorVariants();
            BitThemeColorDerivation.FillColorRoleFromMain(v, seed, BitThemeColorScheme.Dark);

            foreach (var (name, hex) in new[] { ("Main", v.Main!), ("MainHover", v.MainHover!), ("MainActive", v.MainActive!) })
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(v.Text!, hex);
                Assert.IsTrue(
                    ratio >= floor,
                    $"Text {v.Text} on {name} {hex} (dark scheme, seed {seed}) is {ratio:F2}:1, below the {floor}:1 floor");
            }
        }
    }

    // ── Whitespace trimming ───────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainHexWithWhitespaceParsedCorrectly()
    {
        var v1 = new BitThemeColorVariants();
        var v2 = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v1, "#3060A0");
        BitThemeColorDerivation.FillColorRoleFromMain(v2, "  #3060A0  ");
        Assert.AreEqual(v1.Main, v2.Main);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>OKLab lightness (0–1) from a #RRGGBB hex string.</summary>
    private static double OklabLightness(string hex)
    {
        var color = new BitInternalColor(hex);
        var (l, _, _) = BitThemeOklch.FromRgb(color.R, color.G, color.B);
        return l;
    }

    /// <summary>Perceived luminance (0–1) from a #RRGGBB hex string.</summary>
    private static double Luminance(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToInt32(hex[..2], 16);
        var g = Convert.ToInt32(hex[2..4], 16);
        var b = Convert.ToInt32(hex[4..6], 16);
        return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
    }
}
