using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeWithAlphaTests
{
    // ── Output format ─────────────────────────────────────────────────────────

    [TestMethod]
    public void WithAlphaComposesTokenReference()
    {
        Assert.AreEqual(
            "color-mix(in srgb, var(--bit-clr-pri) 12%, transparent)",
            BitThemeUtilities.WithAlpha("var(--bit-clr-pri)", 0.12));
    }

    [TestMethod]
    public void WithAlphaComposesHexLiteral()
    {
        Assert.AreEqual(
            "color-mix(in srgb, #1A86D8 50%, transparent)",
            BitThemeUtilities.WithAlpha("#1A86D8", 0.5));
    }

    [TestMethod]
    public void WithAlphaTrimsSurroundingWhitespace()
    {
        Assert.AreEqual(
            "color-mix(in srgb, currentcolor 20%, transparent)",
            BitThemeUtilities.WithAlpha("  currentcolor  ", 0.2));
    }

    [TestMethod]
    public void WithAlphaFormatsFractionalPercentages()
    {
        Assert.AreEqual(
            "color-mix(in srgb, #000000 12.5%, transparent)",
            BitThemeUtilities.WithAlpha("#000000", 0.125));
    }

    [TestMethod]
    public void WithAlphaBoundsProduceZeroAndFullPercent()
    {
        Assert.AreEqual("color-mix(in srgb, #FFF 0%, transparent)", BitThemeUtilities.WithAlpha("#FFF", 0));
        Assert.AreEqual("color-mix(in srgb, #FFF 100%, transparent)", BitThemeUtilities.WithAlpha("#FFF", 1));
    }

    [TestMethod]
    public void WithAlphaFormattingIsCultureInvariant()
    {
        // A decimal comma ("12,5%") would be invalid CSS AND would collide with color-mix's
        // argument separator. The expression must come out identical under comma-decimal cultures.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.AreEqual(
                "color-mix(in srgb, #000000 12.5%, transparent)",
                BitThemeUtilities.WithAlpha("#000000", 0.125));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [TestMethod]
    public void WithAlphaNullOrBlankColorThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitThemeUtilities.WithAlpha(null!, 0.5));
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeUtilities.WithAlpha("", 0.5));
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeUtilities.WithAlpha("   ", 0.5));
    }

    [TestMethod]
    public void WithAlphaOutOfRangeOpacityThrows()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitThemeUtilities.WithAlpha("#FFF", -0.01));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitThemeUtilities.WithAlpha("#FFF", 1.01));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BitThemeUtilities.WithAlpha("#FFF", double.NaN));
    }

    [TestMethod]
    public void WithAlphaInjectionProneColorThrows()
    {
        // The same screening the mapper applies on emission, surfaced eagerly as an exception
        // instead of a silently dropped token.
        foreach (var evil in new[] { "red;--x:1", "red}body{", "red/*", "url(<script>)", "red\\0060" })
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => BitThemeUtilities.WithAlpha(evil, 0.5),
                $"'{evil}' should have been rejected");
        }
    }

    // ── End-to-end: the composed value is a valid theme token ─────────────────

    [TestMethod]
    public void WithAlphaOutputSurvivesTheMapperScreening()
    {
        var theme = new BitTheme();
        theme.Color.Background.Overlay = BitThemeUtilities.WithAlpha("var(--bit-clr-ntr-black)", 0.4);

        var cssVars = BitThemeUtilities.ToCssVariables(theme);

        var emitted = cssVars.SingleOrDefault(kv => kv.Value.Contains("color-mix", StringComparison.Ordinal));
        Assert.IsNotNull(emitted.Key, "the color-mix token must not be dropped by the mapper's injection screening");
        Assert.AreEqual("color-mix(in srgb, var(--bit-clr-ntr-black) 40%, transparent)", emitted.Value);
    }
}
