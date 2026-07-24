using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeFactoryTests
{
    // ── Light theme ───────────────────────────────────────────────────────────

    [TestMethod]
    public void CreateLightThemeFillsEveryPrimarySlot()
    {
        var theme = BitThemeFactory.CreateLightTheme("#1A86D8");
        var p = theme.Color.Primary;

        Assert.AreEqual("#1A86D8", p.Main);
        foreach (var (name, value) in new[]
        {
            ("MainHover", p.MainHover), ("MainActive", p.MainActive),
            ("Dark", p.Dark), ("DarkHover", p.DarkHover), ("DarkActive", p.DarkActive),
            ("Light", p.Light), ("LightHover", p.LightHover), ("LightActive", p.LightActive),
            ("Text", p.Text), ("Disabled", p.Disabled), ("DisabledText", p.DisabledText), ("Focus", p.Focus),
        })
        {
            Assert.IsNotNull(value, name);
        }
    }

    [TestMethod]
    public void CreateLightThemeNormalizesAccentCasingAndShorthand()
    {
        Assert.AreEqual("#1A86D8", BitThemeFactory.CreateLightTheme("#1a86d8").Color.Primary.Main);
        Assert.AreEqual("#FFFFFF", BitThemeFactory.CreateLightTheme("#fff").Color.Primary.Main);
    }

    [TestMethod]
    public void CreateLightThemeLeavesEverythingElseUntouched()
    {
        // The overlay must stay sparse so packaged stylesheet defaults keep applying: only the
        // seeded role gets values, and only --bit-clr-pri-* variables are emitted end-to-end.
        var theme = BitThemeFactory.CreateLightTheme("#1A86D8");

        Assert.IsNull(theme.Color.Secondary.Main, "Secondary");
        Assert.IsNull(theme.Color.Error.Main, "Error");
        Assert.IsNull(theme.Color.Foreground.Primary, "Foreground");
        Assert.IsNull(theme.Color.Background.Primary, "Background");
        Assert.IsNull(theme.Typography.FontFamily, "Typography");
        Assert.IsNull(theme.Shape.BorderRadius, "Shape");

        var cssVars = BitThemeUtilities.ToCssVariables(theme);
        Assert.IsTrue(cssVars.Count > 0, "some variables must be emitted");
        Assert.IsTrue(cssVars.Keys.All(k => k.StartsWith("--bit-clr-pri", StringComparison.Ordinal)),
            $"only primary variables may be emitted, got: {string.Join(", ", cssVars.Keys.Where(k => !k.StartsWith("--bit-clr-pri", StringComparison.Ordinal)))}");
    }

    // ── Dark theme ────────────────────────────────────────────────────────────

    [TestMethod]
    public void CreateDarkThemeBrightensTheBrandAtConstantHue()
    {
        // The same brand color feeds both schemes; the dark main must come out perceptually
        // lighter (it sits on a dark surface) without drifting hue.
        const string brand = "#1A86D8";
        var dark = BitThemeFactory.CreateDarkTheme(brand).Color.Primary;

        var brandColor = new BitInternalColor(brand);
        var (brandL, _, brandH) = BitThemeOklch.FromRgb(brandColor.R, brandColor.G, brandColor.B);
        var mainColor = new BitInternalColor(dark.Main!);
        var (mainL, _, mainH) = BitThemeOklch.FromRgb(mainColor.R, mainColor.G, mainColor.B);

        Assert.IsTrue(mainL > brandL, $"dark main {dark.Main} must be lighter than brand {brand}");

        var hueDelta = Math.Abs(mainH - brandH);
        if (hueDelta > 180) hueDelta = 360 - hueDelta;
        Assert.IsTrue(hueDelta < 3.0, $"dark main {dark.Main} hue drifted {hueDelta:F1}° from brand {brand}");
    }

    [TestMethod]
    public void CreateDarkThemeMatchesThePackagedDarkPalette()
    {
        // Calibration pin: brand #1A86D8 is the packaged light primary, so the derived dark main
        // must land on (a hue-faithful version of) the packaged dark primary #569FFF. The packaged
        // value carries a hand-tuned ~8° hue shift the derivation intentionally does not copy, so
        // only lightness is pinned.
        var derived = BitThemeFactory.CreateDarkTheme("#1A86D8").Color.Primary.Main!;

        var derivedColor = new BitInternalColor(derived);
        var (derivedL, _, _) = BitThemeOklch.FromRgb(derivedColor.R, derivedColor.G, derivedColor.B);
        var packagedColor = new BitInternalColor("#569FFF");
        var (packagedL, _, _) = BitThemeOklch.FromRgb(packagedColor.R, packagedColor.G, packagedColor.B);

        Assert.IsTrue(Math.Abs(derivedL - packagedL) < 0.02,
            $"derived dark main {derived} (L={derivedL:F4}) should sit within 0.02 OKLab lightness of packaged #569FFF (L={packagedL:F4})");
    }

    [TestMethod]
    public void CreateDarkThemeInteractiveStatesMeetUiContrastFloorForAnyBrand()
    {
        // End-to-end version of the derivation grid test: brighten-for-dark plus dark-scheme
        // derivation must keep the auto-selected Text at ≥ 3.0:1 against the interactive fills for
        // every brand seed on the 6-step RGB grid.
        const double floor = 3.0;

        for (var r = 0; r <= 0xFF; r += 0x33)
        for (var g = 0; g <= 0xFF; g += 0x33)
        for (var b = 0; b <= 0xFF; b += 0x33)
        {
            var brand = FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
            var p = BitThemeFactory.CreateDarkTheme(brand).Color.Primary;

            foreach (var (name, hex) in new[] { ("Main", p.Main!), ("MainHover", p.MainHover!), ("MainActive", p.MainActive!) })
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(p.Text!, hex);
                Assert.IsTrue(
                    ratio >= floor,
                    $"Text {p.Text} on {name} {hex} (brand {brand}) is {ratio:F2}:1, below the {floor}:1 floor");
            }
        }
    }

    // ── Multi-role seeds ──────────────────────────────────────────────────────

    [TestMethod]
    public void CreateLightThemeWithAccentsFillsOnlySeededRoles()
    {
        var theme = BitThemeFactory.CreateLightTheme(new BitThemeAccentColors
        {
            Success = "#107C10",
            Error = "#C50F1F",
        });

        Assert.IsNull(theme.Color.Primary.Main, "Primary must stay untouched");
        Assert.AreEqual("#107C10", theme.Color.Success.Main);
        Assert.IsNotNull(theme.Color.Success.Light);
        Assert.AreEqual("#C50F1F", theme.Color.Error.Main);
        Assert.IsNotNull(theme.Color.Error.Text);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [TestMethod]
    public void CreateLightThemeInvalidAccentThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeFactory.CreateLightTheme("not-a-color"));
    }

    [TestMethod]
    public void CreateDarkThemeInvalidAccentThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeFactory.CreateDarkTheme(""));
    }

    [TestMethod]
    public void CreateLightThemeInvalidOptionalSeedThrowsWithSeedName()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
            BitThemeFactory.CreateLightTheme(new BitThemeAccentColors { Primary = "#1A86D8", Warning = "yellow" }));
        Assert.AreEqual("Warning", ex.ParamName);
    }

    [TestMethod]
    public void CreateLightThemeNullAccentsThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitThemeFactory.CreateLightTheme((BitThemeAccentColors)null!));
    }
}
