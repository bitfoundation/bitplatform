using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// The whole-theme-from-one-seed factory. Two things are being defended here: that seeding the
/// packaged palettes' own primary reproduces them (the calibration pin), and that the WCAG floors
/// those palettes were solved for survive an arbitrary brand color - which is the whole claim of
/// deriving the ladders from fixed anchors and letting the seed supply only hue.
/// </summary>
[TestClass]
public sealed class BitThemeSeedPaletteTests
{
    private const string PackagedPrimary = BitAccentColorPresets.Blue;

    // The floors the packaged palettes document about themselves (see the header of
    // colors.fluent-light.scss), so a generated palette is held to the same bar as a packaged one.
    private const double OnColorFloor = 4.5;
    private const double ForegroundFloor = 4.5;
    private const double TertiaryForegroundFloor = 3.0;
    private const double NonTextFloor = 3.0;

    // A coarse RGB grid plus the shipped presets: enough seeds to catch a family that only clears a
    // floor for blues, cheap enough to stay a unit test.
    private static IEnumerable<string> Seeds()
    {
        foreach (var preset in new[]
        {
            BitAccentColorPresets.Blue, BitAccentColorPresets.Purple, BitAccentColorPresets.Green,
            BitAccentColorPresets.Orange, BitAccentColorPresets.Teal, BitAccentColorPresets.Rose,
        })
        {
            yield return preset;
        }

        for (var r = 0; r <= 0xFF; r += 0x55)
        for (var g = 0; g <= 0xFF; g += 0x55)
        for (var b = 0; b <= 0xFF; b += 0x55)
        {
            yield return FormattableString.Invariant($"#{r:X2}{g:X2}{b:X2}");
        }
    }

    private static (double L, double C, double H) Oklch(string hex)
    {
        var color = new BitInternalColor(hex);
        return BitThemeOklch.FromRgb(color.R, color.G, color.B);
    }

    private static Dictionary<string, string> PackagedTokens(string paletteFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", "Fluent", paletteFile);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library Styles folder is copied to output.");

        return Regex.Matches(File.ReadAllText(path), @"--bit-clr-([a-z0-9-]+):\s*(#[0-9A-Fa-f]{6})")
                    .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.ToUpperInvariant());
    }

    // ── Calibration pin ───────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light, "colors.fluent-light.scss")]
    [DataRow(BitThemeColorScheme.Dark, "colors.fluent-dark.scss")]
    public void SeedingThePackagedPrimaryReproducesThePackagedPaletteExactly(BitThemeColorScheme scheme, string paletteFile)
    {
        // The whole point of transforming the packaged palettes instead of re-deriving them: with the
        // seed on their own primary every transform is the identity, so EVERY token that the theme
        // emits must equal the stylesheet's byte for byte. An earlier version of this generator fitted
        // the ramps from step constants and got 30 of 199 tokens exact, with channel deltas up to 55 -
        // hence this comparing the whole emitted palette rather than a sample of the main slots.
        var packaged = PackagedTokens(paletteFile);
        var theme = scheme is BitThemeColorScheme.Dark
            ? BitThemeFactory.CreateDarkThemeFromSeed(PackagedPrimary)
            : BitThemeFactory.CreateLightThemeFromSeed(PackagedPrimary);

        var generated = BitThemeUtilities.ToCssVariables(theme);

        var failures = new List<string>();
        foreach (var (token, expected) in packaged)
        {
            // PackagedTokens strips the prefix; the mapper emits the full custom-property name.
            if (generated.TryGetValue($"--bit-clr-{token}", out var actual) is false)
            {
                failures.Add($"{token}: not emitted (packaged {expected})");
                continue;
            }

            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) is false)
            {
                failures.Add($"{token}: packaged {expected}, generated {actual}");
            }
        }

        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} of {packaged.Count} tokens differ from {paletteFile}: {string.Join("; ", failures.Take(12))}");
    }

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light)]
    [DataRow(BitThemeColorScheme.Dark)]
    public void SeedingThePackagedPrimaryLeavesTheFocusAliasesAlone(BitThemeColorScheme scheme)
    {
        // The packaged palettes define --bit-clr-*-focus as var() references onto the role they
        // indicate. Emitting a concrete value for them would freeze the indicator to this palette, so
        // the generator leaves them unset and the aliases keep tracking whatever primary becomes.
        var theme = scheme is BitThemeColorScheme.Dark
            ? BitThemeFactory.CreateDarkThemeFromSeed(PackagedPrimary)
            : BitThemeFactory.CreateLightThemeFromSeed(PackagedPrimary);

        var focusVars = BitThemeUtilities.ToCssVariables(theme).Keys.Where(k => k.EndsWith("-focus", StringComparison.Ordinal)).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), focusVars,
            $"focus tokens must stay aliased, got: {string.Join(", ", focusVars)}");
    }

    // ── Completeness ──────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light)]
    [DataRow(BitThemeColorScheme.Dark)]
    public void GeneratedThemeFillsEveryColorSlot(BitThemeColorScheme scheme)
    {
        var theme = scheme is BitThemeColorScheme.Dark
            ? BitThemeFactory.CreateDarkThemeFromSeed(BitAccentColorPresets.Purple)
            : BitThemeFactory.CreateLightThemeFromSeed(BitAccentColorPresets.Purple);

        var unset = new List<string>();

        void checkRole(string name, BitThemeColorVariants v)
        {
            // Focus is intentionally absent - see SeedingThePackagedPrimaryLeavesTheFocusAliasesAlone.
            foreach (var (slot, value) in new[]
            {
                ("Main", v.Main), ("MainHover", v.MainHover), ("MainActive", v.MainActive),
                ("Dark", v.Dark), ("DarkHover", v.DarkHover), ("DarkActive", v.DarkActive),
                ("Light", v.Light), ("LightHover", v.LightHover), ("LightActive", v.LightActive),
                ("Text", v.Text), ("Disabled", v.Disabled), ("DisabledText", v.DisabledText),
            })
            {
                if (value is null) unset.Add($"{name}.{slot}");
            }
        }

        checkRole("Primary", theme.Color.Primary);
        checkRole("Secondary", theme.Color.Secondary);
        checkRole("Tertiary", theme.Color.Tertiary);
        checkRole("Info", theme.Color.Info);
        checkRole("Success", theme.Color.Success);
        checkRole("Warning", theme.Color.Warning);
        checkRole("SevereWarning", theme.Color.SevereWarning);
        checkRole("Error", theme.Color.Error);

        foreach (var (name, family) in new (string, BitThemeGeneralColorVariants)[]
        {
            ("Foreground", theme.Color.Foreground),
            ("Background", theme.Color.Background),
            ("Border", theme.Color.Border),
        })
        {
            foreach (var property in typeof(BitThemeGeneralColorVariants).GetProperties())
            {
                if (property.Name.EndsWith("Focus", StringComparison.Ordinal)) continue;
                if (property.GetValue(family) is null) unset.Add($"{name}.{property.Name}");
            }
        }

        foreach (var property in typeof(BitThemeNeutralColorVariants).GetProperties())
        {
            if (property.GetValue(theme.Color.Neutral) is null) unset.Add($"Neutral.{property.Name}");
        }

        if (theme.Color.Required is null) unset.Add("Required");
        if (theme.Color.Background.Overlay is null) unset.Add("Background.Overlay");

        CollectionAssert.AreEqual(Array.Empty<string>(), unset,
            $"a whole-theme seed must leave no color slot unset: {string.Join(", ", unset)}");
    }

    [TestMethod]
    public void GeneratedThemeEmitsTheWholeColorLayerAndNothingElse()
    {
        // The overlay is a palette, not a restyle: it must not start pinning typography, shapes or
        // motion, and it must not pin the --bit-sem-* alias tier (those are var() references that
        // follow the primitives on their own - pinning would freeze them to this palette).
        var cssVars = BitThemeUtilities.ToCssVariables(BitThemeFactory.CreateLightThemeFromSeed(PackagedPrimary));

        Assert.IsTrue(cssVars.Count > 0);
        var strays = cssVars.Keys.Where(k => k.StartsWith("--bit-clr-", StringComparison.Ordinal) is false).ToArray();
        CollectionAssert.AreEqual(Array.Empty<string>(), strays,
            $"only color variables may be emitted, got: {string.Join(", ", strays)}");
    }

    // ── Contrast, for any seed ────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light)]
    [DataRow(BitThemeColorScheme.Dark)]
    public void OnColorClearsTheUiFloorOverEveryFillForEverySeed(BitThemeColorScheme scheme)
    {
        var failures = new List<string>();

        foreach (var seed in Seeds())
        {
            var theme = scheme is BitThemeColorScheme.Dark
                ? BitThemeFactory.CreateDarkThemeFromSeed(seed)
                : BitThemeFactory.CreateLightThemeFromSeed(seed);

            foreach (var (name, role) in new (string, BitThemeColorVariants)[]
            {
                ("pri", theme.Color.Primary), ("sec", theme.Color.Secondary), ("ter", theme.Color.Tertiary),
                ("inf", theme.Color.Info), ("suc", theme.Color.Success), ("wrn", theme.Color.Warning),
                ("swr", theme.Color.SevereWarning), ("err", theme.Color.Error),
            })
            {
                foreach (var (slot, fill) in new[]
                {
                    ("main", role.Main!), ("hover", role.MainHover!), ("active", role.MainActive!),
                    ("dark", role.Dark!), ("dark-hover", role.DarkHover!), ("dark-active", role.DarkActive!),
                })
                {
                    var ratio = BitThemeColorContrast.GetContrastRatio(fill, role.Text!);
                    if (ratio < OnColorFloor)
                    {
                        failures.Add($"seed {seed} {name}-{slot} {fill} vs on-color {role.Text} = {ratio:F2}:1");
                    }
                }
            }
        }

        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} on-color pairings fell below {OnColorFloor}:1: {string.Join("; ", failures.Take(12))}");
    }

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light)]
    [DataRow(BitThemeColorScheme.Dark)]
    public void ForegroundsAndBordersClearTheirFloorsOverEverySurfaceForEverySeed(BitThemeColorScheme scheme)
    {
        var failures = new List<string>();

        foreach (var seed in Seeds())
        {
            var theme = scheme is BitThemeColorScheme.Dark
                ? BitThemeFactory.CreateDarkThemeFromSeed(seed)
                : BitThemeFactory.CreateLightThemeFromSeed(seed);

            var surfaces = new[]
            {
                ("bg-pri", theme.Color.Background.Primary!),
                ("bg-sec", theme.Color.Background.Secondary!),
                ("bg-ter", theme.Color.Background.Tertiary!),
            };

            var overlays = new[]
            {
                ("fg-pri", theme.Color.Foreground.Primary!, ForegroundFloor),
                ("fg-sec", theme.Color.Foreground.Secondary!, ForegroundFloor),
                ("fg-ter", theme.Color.Foreground.Tertiary!, TertiaryForegroundFloor),
                ("brd-pri", theme.Color.Border.Primary!, NonTextFloor),
            };

            foreach (var (surfaceName, surface) in surfaces)
            foreach (var (overlayName, overlay, floor) in overlays)
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(overlay, surface);
                if (ratio < floor)
                {
                    failures.Add($"seed {seed} {overlayName} {overlay} on {surfaceName} {surface} = {ratio:F2}:1 (floor {floor})");
                }
            }
        }

        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} foreground/surface pairings fell below their floor: {string.Join("; ", failures.Take(12))}");
    }

    [DataTestMethod]
    [DataRow(BitThemeColorScheme.Light)]
    [DataRow(BitThemeColorScheme.Dark)]
    public void DisabledTextClearsThreeToOneOverItsFillForEverySeed(BitThemeColorScheme scheme)
    {
        var failures = new List<string>();

        foreach (var seed in Seeds())
        {
            var theme = scheme is BitThemeColorScheme.Dark
                ? BitThemeFactory.CreateDarkThemeFromSeed(seed)
                : BitThemeFactory.CreateLightThemeFromSeed(seed);

            foreach (var (name, role) in new (string, BitThemeColorVariants)[]
            {
                ("pri", theme.Color.Primary), ("sec", theme.Color.Secondary), ("ter", theme.Color.Tertiary),
                ("inf", theme.Color.Info), ("suc", theme.Color.Success), ("wrn", theme.Color.Warning),
                ("swr", theme.Color.SevereWarning), ("err", theme.Color.Error),
            })
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(role.DisabledText!, role.Disabled!);
                if (ratio < NonTextFloor)
                {
                    failures.Add($"seed {seed} {name}-dis-text {role.DisabledText} on {role.Disabled} = {ratio:F2}:1");
                }
            }
        }

        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} disabled pairings fell below {NonTextFloor}:1: {string.Join("; ", failures.Take(12))}");
    }

    // ── Behavior of the knobs ─────────────────────────────────────────────────

    [TestMethod]
    public void HarmonizationLeansStatusHuesTowardTheSeedWithoutLosingThem()
    {
        // A green seed must not turn "error" green. The rotation is capped, so the error hue stays
        // firmly in the red arc however far the seed is from it.
        var harmonized = BitThemeFactory
            .CreateLightThemeFromSeed(BitAccentColorPresets.Green, new BitThemeSeedOptions { SemanticHarmonizationDegrees = 15 })
            .Color.Error.Main!;
        var canonical = BitThemeFactory.CreateLightThemeFromSeed(BitAccentColorPresets.Green).Color.Error.Main!;

        var harmonizedHue = Oklch(harmonized).H;
        var canonicalHue = Oklch(canonical).H;

        Assert.AreNotEqual(canonical, harmonized, "asking for harmonization must actually harmonize");

        var rotation = Math.Abs(harmonizedHue - canonicalHue);
        if (rotation > 180) rotation = 360 - rotation;
        Assert.IsTrue(rotation <= 15.5, $"error hue rotated {rotation:F1}°, above the 15° cap");

        // Reds live around 20-40° in OKLCH; a capped rotation cannot leave that neighborhood.
        Assert.IsTrue(harmonizedHue is > 0 and < 60, $"harmonized error hue {harmonizedHue:F1}° is no longer a red");
    }

    [TestMethod]
    public void WithoutATintTheLightSchemeSurfacesStayPureGray()
    {
        // The default adds no chroma, so anything the packaged light palette left achromatic stays
        // achromatic however far the seed's hue is from blue. (The gray ramp is deliberately not in
        // this list: Fluent's ramp carries its own warm tint, and that tint turns with the brand like
        // any other chroma the palette already had.)
        var theme = BitThemeFactory.CreateLightThemeFromSeed(BitAccentColorPresets.Rose);

        foreach (var (name, hex) in new[]
        {
            ("bg-pri", theme.Color.Background.Primary!), ("bg-sec", theme.Color.Background.Secondary!),
            ("fg-pri", theme.Color.Foreground.Primary!), ("brd-pri", theme.Color.Border.Primary!),
        })
        {
            var color = new BitInternalColor(hex);
            Assert.IsTrue(color.R == color.G && color.G == color.B, $"{name} {hex} is not a pure gray");
        }
    }

    [TestMethod]
    public void NeutralTintPullsSurfacesOntoTheSeedHue()
    {
        var seedHue = Oklch(BitAccentColorPresets.Rose).H;
        var surface = BitThemeFactory
            .CreateLightThemeFromSeed(BitAccentColorPresets.Rose, new BitThemeSeedOptions { NeutralTintChroma = 0.012 })
            .Color.Background.Secondary!;

        var (_, chroma, hue) = Oklch(surface);

        Assert.IsTrue(chroma > 0, $"the tinted surface {surface} should carry some chroma");

        var delta = Math.Abs(hue - seedHue);
        if (delta > 180) delta = 360 - delta;
        Assert.IsTrue(delta < 12.0, $"tinted surface {surface} (H={hue:F1}) drifted from the seed hue {seedHue:F1}");
    }

    [TestMethod]
    public void TheDarkSchemeCarriesItsPackagedTintOntoTheSeedHue()
    {
        // The packaged dark neutrals are already tinted toward the primary, so a seeded dark theme
        // gets brand-tinted surfaces with no options at all - the tint just turns with the brand.
        var seedHue = Oklch(BitAccentColorPresets.Rose).H;
        var surface = BitThemeFactory.CreateDarkThemeFromSeed(BitAccentColorPresets.Rose).Color.Background.Secondary!;

        var (_, chroma, hue) = Oklch(surface);

        Assert.IsTrue(chroma > 0.005, $"the dark surface {surface} should keep the packaged tint");

        var delta = Math.Abs(hue - seedHue);
        if (delta > 180) delta = 360 - delta;
        Assert.IsTrue(delta < 12.0, $"dark surface {surface} (H={hue:F1}) drifted from the seed hue {seedHue:F1}");
    }

    [TestMethod]
    public void IncludeNeutralsFalseLeavesSurfacesToTheStylesheet()
    {
        var theme = BitThemeFactory.CreateLightThemeFromSeed(
            BitAccentColorPresets.Teal, new BitThemeSeedOptions { IncludeNeutrals = false });

        Assert.IsNull(theme.Color.Background.Primary, "Background");
        Assert.IsNull(theme.Color.Foreground.Primary, "Foreground");
        Assert.IsNull(theme.Color.Border.Primary, "Border");
        Assert.IsNull(theme.Color.Neutral.Gray70, "Neutral ramp");

        Assert.IsNotNull(theme.Color.Primary.Main, "the accents must still be generated");
        Assert.IsNotNull(theme.Color.Error.Main, "the status roles must still be generated");
    }

    [TestMethod]
    public void SecondaryHueShiftMovesTheSecondAccent()
    {
        var seedHue = Oklch(BitAccentColorPresets.Blue).H;
        var theme = BitThemeFactory.CreateLightThemeFromSeed(
            BitAccentColorPresets.Blue, new BitThemeSeedOptions { SecondaryHueShift = 90 });

        var secondaryHue = Oklch(theme.Color.Secondary.Main!).H;
        var expected = (seedHue + 90) % 360;

        var delta = Math.Abs(secondaryHue - expected);
        if (delta > 180) delta = 360 - delta;
        Assert.IsTrue(delta < 4.0, $"secondary hue {secondaryHue:F1}° should sit 90° from the seed ({expected:F1}°)");
    }

    [TestMethod]
    public void DarkSeedThemeLandsTheBrandOnTheDarkSurfaceAtItsOwnHue()
    {
        // One brand color feeds both schemes. The whole-theme factory gets there by transforming the
        // packaged dark palette rather than by brightening the brand, so it does not have to agree
        // token-for-token with CreateDarkTheme - but it must still put the accent where a dark scheme
        // needs it: lighter than the brand, and on the brand's hue.
        const string brand = BitAccentColorPresets.Purple;

        var main = BitThemeFactory.CreateDarkThemeFromSeed(brand).Color.Primary.Main!;

        var (brandL, _, brandH) = Oklch(brand);
        var (mainL, _, mainH) = Oklch(main);

        Assert.IsTrue(mainL > brandL, $"dark main {main} must be lighter than the brand {brand}");

        var hueDelta = Math.Abs(mainH - brandH);
        if (hueDelta > 180) hueDelta = 360 - hueDelta;
        Assert.IsTrue(hueDelta < 3.0, $"dark main {main} drifted {hueDelta:F1}° from the brand {brand}");
    }

    [TestMethod]
    public void LightSeedThemeKeepsTheBrandColorExactly()
    {
        Assert.AreEqual("#8764B8", BitThemeFactory.CreateLightThemeFromSeed("#8764b8").Color.Primary.Main);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [TestMethod]
    public void InvalidSeedThrows()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() => BitThemeFactory.CreateLightThemeFromSeed("not-a-color"));
        Assert.AreEqual("seedHex", ex.ParamName);

        Assert.ThrowsExactly<ArgumentException>(() => BitThemeFactory.CreateDarkThemeFromSeed(""));
    }
}
