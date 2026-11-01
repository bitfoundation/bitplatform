using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void RoleOnColorsMeetTheUiContrastFloor(string paletteFolder, string paletteFile)
    {
        var scss = SourceFiles.ReadThemeStylesheet(paletteFolder, paletteFile);

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

    // The surfaces a role is read on when it is not filled - the label of an outlined or text-only variant - with the
    // floor its text keeps over each: AA normal text over the page and the raised surfaces, and the 3:1 of large text
    // over the deepest one, the same allowance fg-ter has there.
    private static readonly (string Surface, double Floor)[] PageSurfaces = [("bg-pri", 4.5), ("bg-sec", 4.5), ("bg-ter", 3.0)];

    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void RoleTextColorsMeetTheTextContrastFloorOnThePage(string paletteFolder, string paletteFile)
    {
        var scss = SourceFiles.ReadThemeStylesheet(paletteFolder, paletteFile);

        string token(string name)
        {
            var match = Regex.Match(scss, $@"--bit-clr-{name}:\s*(#[0-9A-Fa-f]{{6}})");
            Assert.IsTrue(match.Success, $"--bit-clr-{name} not found in {paletteFile}.");
            return match.Groups[1].Value;
        }

        // The packaged palettes leave the role foreground to the shared recipe, so it is mixed here the way the browser
        // mixes it: the share of the main is read off the recipe itself, so a retuned recipe is held to the floor too.
        var recipe = Regex.Match(SourceFiles.Read("Bit.BlazorUI", "Styles", "family-tokens.scss"),
                                 @"--bit-clr-#\{\$role\}-fg: color-mix\(in srgb, var\(--bit-clr-#\{\$role\}\) (\d+)%, var\(--bit-clr-fg-pri\)\);");
        Assert.IsTrue(recipe.Success, "The role foreground recipe was not found in family-tokens.scss.");
        Assert.IsFalse(Regex.IsMatch(scss, $@"--bit-clr-(?:{string.Join("|", Roles)})-fg:"), $"{paletteFile} declares a role foreground of its own, which this test does not read.");
        var share = int.Parse(recipe.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;

        var failures = new List<string>();
        foreach (var role in Roles)
        {
            // role-text (color-role-maps.scss) reads the primary main as it is - the brand's link color - and every
            // other role in its foreground shade.
            var text = role == "pri" ? token(role) : Mix(token(role), token("fg-pri"), share);
            var surfaces = role == "pri" ? PageSurfaces.Take(1) : PageSurfaces;

            foreach (var (surface, floor) in surfaces)
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(text, token(surface));
                if (ratio < floor)
                {
                    failures.Add($"{role} text {text} vs --bit-clr-{surface} = {ratio:F2}:1 (floor {floor}:1)");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has roles whose text color is unreadable on the page: {string.Join("; ", failures)}");
    }

    // color-mix(in srgb, a share%, b): each gamma-encoded channel interpolated, as the browser does it.
    private static string Mix(string a, string b, double share)
    {
        int channel(string hex, int index) => int.Parse(hex.AsSpan(1 + index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return "#" + string.Concat(Enumerable.Range(0, 3).Select(i => ((int)Math.Round(channel(a, i) * share + channel(b, i) * (1 - share))).ToString("X2", CultureInfo.InvariantCulture)));
    }
}
