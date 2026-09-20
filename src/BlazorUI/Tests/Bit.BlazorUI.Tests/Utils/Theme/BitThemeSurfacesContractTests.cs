using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// BitThemeSurfaces is the one place the library restates a color its own
/// stylesheets already declare, because a host page needs it before any stylesheet has loaded (see
/// BitThemeHead). A re-generated palette would leave those literals behind with nothing failing: the
/// page would simply paint the browser chrome in the previous release's color, a seam only visible on
/// a device. These tests read the packaged palettes themselves so that drift fails the build.
/// </summary>
[TestClass]
public sealed class BitThemeSurfacesContractTests
{
    /// <summary>Preset name, the palette folder under theme-styles, and its stylesheet.</summary>
    private static IEnumerable<object[]> Palettes =>
    [
        [BitThemePresets.Light, "Fluent", "colors.fluent-light.scss"],
        [BitThemePresets.Dark, "Fluent", "colors.fluent-dark.scss"],
        // Each family's base alias is selected by its light palette, so it is pinned to that file.
        [BitThemePresets.Fluent, "Fluent", "colors.fluent-light.scss"],
        [BitThemePresets.FluentLight, "Fluent", "colors.fluent-light.scss"],
        [BitThemePresets.FluentDark, "Fluent", "colors.fluent-dark.scss"],
        [BitThemePresets.Fluent2, "Fluent2", "colors.fluent2-light.scss"],
        [BitThemePresets.Fluent2Light, "Fluent2", "colors.fluent2-light.scss"],
        [BitThemePresets.Fluent2Dark, "Fluent2", "colors.fluent2-dark.scss"],
        [BitThemePresets.Material, "Material", "colors.material-light.scss"],
        [BitThemePresets.MaterialLight, "Material", "colors.material-light.scss"],
        [BitThemePresets.MaterialDark, "Material", "colors.material-dark.scss"],
        [BitThemePresets.Cupertino, "Cupertino", "colors.cupertino-light.scss"],
        [BitThemePresets.CupertinoLight, "Cupertino", "colors.cupertino-light.scss"],
        [BitThemePresets.CupertinoDark, "Cupertino", "colors.cupertino-dark.scss"],
    ];

    [TestMethod]
    [DynamicData(nameof(Palettes))]
    public void SurfaceColorsMatchThePackagedPalettes(string preset, string paletteFolder, string paletteFile)
    {
        var scss = ReadPalette(paletteFolder, paletteFile);

        AssertSurface(BitThemeSurfaces.BackgroundPrimary, preset, ReadVariable(scss, "--bit-clr-bg-pri", paletteFile), "--bit-clr-bg-pri");
        AssertSurface(BitThemeSurfaces.BackgroundSecondary, preset, ReadVariable(scss, "--bit-clr-bg-sec", paletteFile), "--bit-clr-bg-sec");
    }

    [TestMethod]
    public void EveryPackagedPresetHasBothSurfaces()
    {
        // A preset named on BitThemePresets but never registered with BitThemePresetRegistry would
        // fall back to the scheme's Fluent surface at first paint - a wrong color, silently. System
        // is excluded: it is a pseudo-preset that resolves to another name before any color is
        // looked up. Reading the Extras names below is also what loads that package, and so what
        // registers its presets - the same thing an app's own code does.
        string[] presets =
        [
            BitThemePresets.Light, BitThemePresets.Dark,
            BitThemePresets.Fluent, BitThemePresets.FluentLight, BitThemePresets.FluentDark,
            BitThemePresets.Fluent2, BitThemePresets.Fluent2Light, BitThemePresets.Fluent2Dark,
            BitThemePresets.Material, BitThemePresets.MaterialLight, BitThemePresets.MaterialDark,
            BitThemePresets.Cupertino, BitThemePresets.CupertinoLight, BitThemePresets.CupertinoDark,
        ];

        foreach (var preset in presets)
        {
            Assert.IsTrue(BitThemeSurfaces.BackgroundPrimary.ContainsKey(preset), $"BackgroundPrimary has no entry for {preset}.");
            Assert.IsTrue(BitThemeSurfaces.BackgroundSecondary.ContainsKey(preset), $"BackgroundSecondary has no entry for {preset}.");
        }
    }

    private static void AssertSurface(IReadOnlyDictionary<string, string> table, string preset, string expected, string variable)
    {
        Assert.IsTrue(table.TryGetValue(preset, out var actual), $"No {variable} entry for {preset}.");
        Assert.AreEqual(expected, actual, true,
            $"{preset} declares {variable}: {expected} in its palette, but the table says {actual}. The table is what a host page paints the browser chrome with before any stylesheet has loaded.");
    }

    private static string ReadPalette(string paletteFolder, string paletteFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", paletteFolder, paletteFile);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library Styles folders are copied to output (see Bit.BlazorUI.Tests.csproj).");

        return File.ReadAllText(path);
    }

    private static string ReadVariable(string scss, string variable, string paletteFile)
    {
        // The first declaration is the palette's own :root block; later ones (forced-colors and the
        // like) live in other files.
        var match = Regex.Match(scss, $@"{Regex.Escape(variable)}:\s*(#[0-9A-Fa-f]{{3,8}})\s*;");
        Assert.IsTrue(match.Success, $"{paletteFile} no longer declares {variable} as a hex literal.");

        return match.Groups[1].Value;
    }
}
