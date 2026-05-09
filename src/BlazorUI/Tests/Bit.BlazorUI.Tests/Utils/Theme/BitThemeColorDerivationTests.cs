using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeColorDerivationTests
{
    // ── Guard clauses ──────────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_NullVariants_DoesNotThrow()
    {
        // Should return silently – no exception expected.
        BitThemeColorDerivation.FillColorRoleFromMain(null!, "#FF0000");
    }

    [TestMethod]
    public void FillColorRoleFromMain_NullHex_DoesNotThrow()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, null!);
        Assert.IsNull(v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMain_EmptyHex_DoesNotThrow()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "");
        Assert.IsNull(v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMain_InvalidHex_DoesNotThrow()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "not-a-color");
        // BitInternalColor silently falls back to white when the format is unrecognised
        // (its own catch resets R/G/B to 255 without rethrowing), so FillColorRoleFromMain's
        // outer catch never fires and all variants are populated with white-derived values.
        Assert.IsNotNull(v.Main,        "Main");
        Assert.IsNotNull(v.Dark,        "Dark");
        Assert.IsNotNull(v.Light,       "Light");
        Assert.IsNotNull(v.Text,        "Text");
        Assert.IsNotNull(v.MainHover,   "MainHover");
        Assert.IsNotNull(v.MainActive,  "MainActive");
        Assert.IsNotNull(v.DarkHover,   "DarkHover");
        Assert.IsNotNull(v.DarkActive,  "DarkActive");
        Assert.IsNotNull(v.LightHover,  "LightHover");
        Assert.IsNotNull(v.LightActive, "LightActive");
    }

    // ── All slots populated ────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_ValidColor_AllVariantsPopulated()
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
    public void FillColorRoleFromMain_PresetMainNotOverwritten()
    {
        const string preset = "#AABBCC";
        var v = new BitThemeColorVariants { Main = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#FF0000");
        Assert.AreEqual(preset, v.Main);
    }

    [TestMethod]
    public void FillColorRoleFromMain_PresetLightNotOverwritten()
    {
        const string preset = "#FFFFFF";
        var v = new BitThemeColorVariants { Light = preset };
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");
        Assert.AreEqual(preset, v.Light);
    }

    // ── Hex format ────────────────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_ValidColor_HexValuesStartWithHash()
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
        })
        {
            Assert.IsTrue(value!.StartsWith('#'), $"{name} should start with '#' but was '{value}'");
        }
    }

    // ── Dark variants are darker than Main ────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_DarkVariants_AreDarkerThanMain()
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
    public void FillColorRoleFromMain_LightVariants_AreLighterThanMain()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#3060A0");

        var mainLum  = Luminance(v.Main!);
        var lightLum = Luminance(v.Light!);

        Assert.IsTrue(lightLum > mainLum, "Light should be lighter than Main");
    }

    // ── Light steps are distinct even for high-brightness colors ──────────────

    [TestMethod]
    public void FillColorRoleFromMain_HighBrightnessColor_LightStepsAreDistinct()
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
    public void FillColorRoleFromMain_HighBrightnessColor_LightStepsMutuallyDistinct()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#B0C8E0"); // mid-high brightness

        Assert.AreNotEqual(v.Light,      v.LightHover,  "Light and LightHover must differ");
        Assert.AreNotEqual(v.LightHover, v.LightActive, "LightHover and LightActive must differ");
        Assert.AreNotEqual(v.Light,      v.LightActive, "Light and LightActive must differ");
    }

    [TestMethod]
    public void FillColorRoleFromMain_VeryHighBrightnessColor_LightStepsMutuallyDistinct()
    {
        // #D0D0D0 → v ≈ 0.816; all three additive steps (0.08/0.12/0.16) stay below 1.0
        // and produce distinct hex values.  Colors with v > 0.84 may still have LightActive
        // clamp to white — that is an inherent ceiling, not a regression.
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#D0D0D0"); // v ≈ 0.816

        Assert.AreNotEqual(v.Light,      v.LightHover,  "Light and LightHover must differ at high brightness");
        Assert.AreNotEqual(v.LightHover, v.LightActive, "LightHover and LightActive must differ at high brightness");
    }

    // ── Text contrast suggestion ───────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_DarkBaseColor_TextIsWhite()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#1A1A2E");
        Assert.AreEqual("#FFFFFF", v.Text);
    }

    [TestMethod]
    public void FillColorRoleFromMain_LightBaseColor_TextIsBlack()
    {
        var v = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v, "#F0F0F0");
        Assert.AreEqual("#000000", v.Text);
    }

    // ── Whitespace trimming ───────────────────────────────────────────────────

    [TestMethod]
    public void FillColorRoleFromMain_HexWithWhitespace_ParsedCorrectly()
    {
        var v1 = new BitThemeColorVariants();
        var v2 = new BitThemeColorVariants();
        BitThemeColorDerivation.FillColorRoleFromMain(v1, "#3060A0");
        BitThemeColorDerivation.FillColorRoleFromMain(v2, "  #3060A0  ");
        Assert.AreEqual(v1.Main, v2.Main);
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
