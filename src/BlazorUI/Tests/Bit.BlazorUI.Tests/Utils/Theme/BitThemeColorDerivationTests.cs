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

        Assert.IsTrue(lightLum   > mainLum,   "Light should be lighter than Main");
        Assert.IsTrue(lHoverLum  > lightLum,  "LightHover should be lighter than Light");
        Assert.IsTrue(lActiveLum > lHoverLum, "LightActive should be lighter than LightHover");
    }

    // ── Light steps are distinct even for high-brightness colors ──────────────

    [TestMethod]
    public void FillColorRoleFromMainHighBrightnessColorLightStepsAreDistinct()
    {
        // Pure white or near-white causes multiplicative scaling to collapse;
        // additive offsets must keep steps distinguishable.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#E8E8E8"); // high-v grey

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
        // #D0D0D0 → v ≈ 0.816; all three additive steps (0.08/0.12/0.16) stay below 1.0
        // and produce distinct hex values.  Colors with v > 0.84 may still have LightActive
        // clamp to white - that is an inherent ceiling, not a regression.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#D0D0D0"); // v ≈ 0.816

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

    // ── adjustTextForWcagAa ───────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaDefaultFalseKeepsHeuristicSuggestion()
    {
        // #888888: heuristic luminance ≈ 0.533 → suggests "#FFFFFF".
        // Without the flag, that suggestion stands even though WCAG AA prefers black.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#888888");
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaFlipsToBlackOnMidGray()
    {
        // #888888 vs white ≈ 3.55 (fails AA), vs black ≈ 5.92 (passes AA).
        // Heuristic picks white; flag must flip to black via pass/fail precedence.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#888888", adjustTextForWcagAa: true);
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaFlipsToBlackOnSaturatedColor()
    {
        // #009100 has a low heuristic luminance (≈ 0.334) so the heuristic suggests white,
        // but WCAG relative luminance ≈ 0.20: white fails AA (≈ 4.16), black passes (≈ 5.05).
        // Pass/fail precedence must select the AA-passing candidate (black).
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#009100", adjustTextForWcagAa: true);
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaKeepsWhiteOnVeryDarkColor()
    {
        // #1A1A2E: only white passes AA. Heuristic also picks white. No flip.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#1A1A2E", adjustTextForWcagAa: true);
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaKeepsBlackOnVeryLightColor()
    {
        // #F0F0F0: only black passes AA. Heuristic also picks black. No flip.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0", adjustTextForWcagAa: true);
        Assert.AreEqual("#000000", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaTieBreakPicksHigherContrast()
    {
        // #757575: both candidates pass AA (black ≈ 4.558, white ≈ 4.607).
        // Tie-break must select white as the higher-contrast option.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#757575", adjustTextForWcagAa: true);
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMainAdjustTextForWcagAaPresetTextNotOverwritten()
    {
        // Caller-provided Text must never be overwritten — even when it would fail WCAG AA.
        const string preset = "#FFFFFF"; // would fail AA on #888888
        var v = new BitThemeColorVariants { Text = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#888888", adjustTextForWcagAa: true);
        Assert.AreEqual(preset, v.Text);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
