using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeContrastPreferenceTests
{
    // Guards against the prefers-contrast block silently becoming a no-op: it once "bumped" the
    // border width to 1px while the default was 0.0625rem - the exact same length.
    [TestMethod]
    public void PrefersContrastMoreActuallyChangesTheBorderWidth()
    {
        var stylesDir = Path.Combine(AppContext.BaseDirectory, "theme-styles", "Fluent");
        var shapes = File.ReadAllText(Path.Combine(stylesDir, "shapes.fluent.scss"));
        var forced = File.ReadAllText(Path.Combine(stylesDir, "forced-colors.fluent.scss"));

        var defaultWidth = ExtractBorderWidthPx(shapes);
        var contrastBlock = Regex.Match(forced, @"prefers-contrast:\s*more\)\s*\{(?<body>[\s\S]*?)\n\}").Groups["body"].Value;
        Assert.AreNotEqual(string.Empty, contrastBlock, "prefers-contrast: more block not found in forced-colors.fluent.scss.");
        var contrastWidth = ExtractBorderWidthPx(contrastBlock);

        Assert.AreNotEqual(defaultWidth, contrastWidth,
            $"prefers-contrast: more sets --bit-shp-brd-width to the same length as the default ({defaultWidth}px) - the override is a no-op.");
        Assert.IsTrue(contrastWidth > defaultWidth, "prefers-contrast: more should thicken borders, not thin them.");
    }

    private static double ExtractBorderWidthPx(string scss)
    {
        var match = Regex.Match(scss, @"--bit-shp-brd-width:\s*([\d.]+)(px|rem)");
        Assert.IsTrue(match.Success, "--bit-shp-brd-width declaration not found.");

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return match.Groups[2].Value == "rem" ? value * 16 : value;
    }
}
