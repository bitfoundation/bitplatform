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

    // Every fill state the role's -text on-color is actually painted over. Besides the main tier
    // (filled buttons, tags, snackbars) that includes the dark tier, which BitToggleButton's checked
    // state and BitPagination's selected page fill with while still drawing the on-color as text.
    private static readonly string[] FillSuffixes = ["", "-hover", "-active", "-dark", "-dark-hover", "-dark-active"];

    // The palettes are solved so the on-color clears AA normal text on every fill it lands on, so
    // the gate is the full 4.5 rather than the 1.4.11 UI floor of 3.0 - there are no longer any
    // deliberate exceptions sitting between the two. This is the regression guard for that: it is
    // what stops a future palette tweak from quietly reintroducing an unreadable pairing (white on
    // warning yellow was once 1.91:1, and the on-color over the dark tier once fell to 2.34:1).
    private const double UiContrastFloor = 4.5;

    [DataTestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void RoleOnColorsMeetTheUiContrastFloor(string paletteFolder, string paletteFile)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", paletteFolder, paletteFile);
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
