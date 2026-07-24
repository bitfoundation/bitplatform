using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemePaletteContrastTests
{
    private static readonly string[] Roles = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err"];

    // Fill states that components pair with the role's -text on-color (e.g. filled buttons).
    private static readonly string[] FillSuffixes = ["", "-hover", "-active"];

    // 3.0 is the WCAG 1.4.11/1.4.3 large-text & UI-component floor, used as the hard gate so a
    // palette tweak can never reintroduce unreadable combos (white on warning yellow was 1.91:1).
    // 4.5 (AA normal text) is the target; the known deliberate exceptions below sit in between:
    // light pri (#1A86D8 + white = 3.86) and err (#DF3A2E + white = 4.39), where white still beats
    // any dark on-color and matches platform conventions for brand/danger fills.
    private const double UiContrastFloor = 3.0;

    [DataTestMethod]
    [DataRow("colors.fluent-light.scss")]
    [DataRow("colors.fluent-dark.scss")]
    public void RoleOnColorsMeetTheUiContrastFloor(string paletteFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", "Fluent", paletteFile);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library Styles folder is copied to output.");

        var scss = File.ReadAllText(path);

        string? token(string name)
        {
            var match = Regex.Match(scss, $@"--bit-clr-{name}:\s*(#[0-9A-Fa-f]{{6}})");
            return match.Success ? match.Groups[1].Value : null;
        }

        var failures = new List<string>();
        foreach (var role in Roles)
        {
            var text = token($"{role}-text");
            Assert.IsNotNull(text, $"--bit-clr-{role}-text not found in {paletteFile}.");

            foreach (var suffix in FillSuffixes)
            {
                var fill = token($"{role}{suffix}");
                Assert.IsNotNull(fill, $"--bit-clr-{role}{suffix} not found in {paletteFile}.");

                var ratio = BitThemeColorContrast.GetContrastRatio(fill, text);
                if (ratio < UiContrastFloor)
                {
                    failures.Add($"--bit-clr-{role}{suffix} {fill} vs on-color {text} = {ratio:F2}:1");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has role fills whose on-color falls below {UiContrastFloor}:1: {string.Join("; ", failures)}");
    }
}
