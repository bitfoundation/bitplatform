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

    // Every role's foreground, the primary one's included: what role-text (color-role-maps.scss) draws every role but the
    // primary one in, and what BitLabel, BitMessage and the other components that print a role as text read for all of them.
    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void RoleForegroundsMeetTheTextContrastFloorOnThePage(string paletteFolder, string paletteFile)
    {
        var palette = ReadPalette(paletteFolder, paletteFile);

        var failures = new List<string>();
        foreach (var role in Roles)
        {
            var text = palette.Foreground(role);
            foreach (var (surface, floor) in PageSurfaces)
            {
                var ratio = BitThemeColorContrast.GetContrastRatio(text, palette.Token(surface));
                if (ratio < floor)
                {
                    failures.Add($"--bit-clr-{role}-fg {text} vs --bit-clr-{surface} = {ratio:F2}:1 (floor {floor}:1)");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has role foregrounds that are unreadable on the page: {string.Join("; ", failures)}");
    }

    // The one role role-text reads in its main: the brand's link color, which holds AA normal text over the page and the 3:1
    // of large text over the raised surfaces.
    private static readonly (string Surface, double Floor)[] PrimaryMainSurfaces = [("bg-pri", 4.5), ("bg-sec", 3.0), ("bg-ter", 3.0)];

    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void ThePrimaryMainMeetsTheTextContrastFloorOnThePage(string paletteFolder, string paletteFile)
    {
        var palette = ReadPalette(paletteFolder, paletteFile);
        var text = palette.Token("pri");

        var failures = new List<string>();
        foreach (var (surface, floor) in PrimaryMainSurfaces)
        {
            var ratio = BitThemeColorContrast.GetContrastRatio(text, palette.Token(surface));
            if (ratio < floor)
            {
                failures.Add($"--bit-clr-pri {text} vs --bit-clr-{surface} = {ratio:F2}:1 (floor {floor}:1)");
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has a primary main that is unreadable as text on the page: {string.Join("; ", failures)}");
    }

    // The surface an outlined or text-only tag is selected and hovered on, and a clickable outlined badge is hovered on: the
    // role's tint laid over the page (role-wash in color-role-maps.scss), with the role's foreground read on top - the
    // primary one's too, whose main drops under the floor there. The deeper wash of a pointer pressing an unselected one is
    // transient and is not held to it; a pointer on a selected one is, below.
    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void RoleForegroundsMeetTheTextContrastFloorOverTheirTint(string paletteFolder, string paletteFile)
    {
        var palette = ReadPalette(paletteFolder, paletteFile);

        var failures = new List<string>();
        foreach (var role in Roles)
        {
            var text = palette.Foreground(role);
            var tint = palette.TintOver(role, "bg-pri");
            var ratio = BitThemeColorContrast.GetContrastRatio(text, tint);
            if (ratio < 4.5)
            {
                failures.Add($"--bit-clr-{role}-fg {text} vs --bit-clr-{role}-tint over --bit-clr-bg-pri ({tint}) = {ratio:F2}:1 (floor 4.5:1)");
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has role foregrounds that are unreadable on their tint: {string.Join("; ", failures)}");
    }

    // A selected outlined or text-only tag under the pointer: the pointer's wash - the tint while it rests there, a deeper wash
    // of the main while it presses - laid over the selection's, past the tint the role's foreground is solved for (a warning
    // amber's drops under the floor there). Its text is that foreground leaned towards the page's text (deep-wash-text in
    // color-role-maps.scss), held to the floor on both.
    [TestMethod]
    [DataRow("Fluent", "colors.fluent-light.scss")]
    [DataRow("Fluent", "colors.fluent-dark.scss")]
    [DataRow("Fluent2", "colors.fluent2-light.scss")]
    [DataRow("Fluent2", "colors.fluent2-dark.scss")]
    [DataRow("Material", "colors.material-light.scss")]
    [DataRow("Material", "colors.material-dark.scss")]
    [DataRow("Cupertino", "colors.cupertino-light.scss")]
    [DataRow("Cupertino", "colors.cupertino-dark.scss")]
    public void DeeperRoleForegroundsMeetTheTextContrastFloorUnderAPointerOnTheirSelectedTint(string paletteFolder, string paletteFile)
    {
        var palette = ReadPalette(paletteFolder, paletteFile);
        var deeper = Share(SourceFiles.Read("Bit.BlazorUI", "Styles", "color-role-maps.scss"),
                           @"@function deep-wash-text\(\$text\) \{\s*@return color-mix\(in srgb, #\{\$text\} (\d+)%, #\{\$clr-fg-pri\}\);", "deep-wash-text");
        var pressed = Share(SourceFiles.Read("Bit.BlazorUI", "Components", "Notifications", "Tag", "BitTag.scss"),
                            @"--bit-tag-clr-tint-active: #\{role-wash\(\$tokens, (\d+)%\)\};", "pressed wash");

        var failures = new List<string>();
        foreach (var role in Roles)
        {
            var text = Mix(palette.Foreground(role), palette.Token("fg-pri"), deeper);
            var selected = palette.TintOver(role, "bg-pri");
            foreach (var (state, share) in new[] { ("hovered", palette.TintShare), ("pressed", pressed) })
            {
                var wash = Mix(palette.Token(role), selected, share);
                var ratio = BitThemeColorContrast.GetContrastRatio(text, wash);
                if (ratio < 4.5)
                {
                    failures.Add($"--bit-clr-{role}-fg leaned deeper {text} vs a {state} selected tint ({wash}) = {ratio:F2}:1 (floor 4.5:1)");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), failures,
            $"{paletteFile} has role foregrounds that are unreadable under a pointer on a selected tint: {string.Join("; ", failures)}");
    }

    // The share of a color in a recipe, read off the stylesheet that writes it.
    private static double Share(string source, string recipe, string name)
    {
        var match = Regex.Match(source, recipe);
        Assert.IsTrue(match.Success, $"The {name} recipe was not found.");
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
    }

    private sealed record Palette(string File, string Scss, double ForegroundShare, double TintShare)
    {
        public string Token(string name)
        {
            var match = Regex.Match(Scss, $@"--bit-clr-{name}:\s*(#[0-9A-Fa-f]{{6}})");
            Assert.IsTrue(match.Success, $"--bit-clr-{name} not found in {File}.");
            return match.Groups[1].Value;
        }

        // The packaged palettes leave the role foreground to the shared recipe, so it is mixed the way the browser mixes it.
        public string Foreground(string role) => Mix(Token(role), Token("fg-pri"), ForegroundShare);

        // The tint is the main mixed with transparent, which composited over an opaque surface is the main mixed with it.
        public string TintOver(string role, string surface) => Mix(Token(role), Token(surface), TintShare);
    }

    // The shares of the main in the two family recipes are read off the recipes themselves, so a retuned recipe is held to
    // the floors too.
    private static Palette ReadPalette(string paletteFolder, string paletteFile)
    {
        var scss = SourceFiles.ReadThemeStylesheet(paletteFolder, paletteFile);
        var familyTokens = SourceFiles.Read("Bit.BlazorUI", "Styles", "family-tokens.scss");

        double share(string recipe, string name) => Share(familyTokens, recipe, $"role {name}");

        foreach (var family in new[] { "fg", "tint" })
        {
            Assert.IsFalse(Regex.IsMatch(scss, $@"--bit-clr-(?:{string.Join("|", Roles)})-{family}:"), $"{paletteFile} declares a role {family} of its own, which this test does not read.");
        }

        return new(paletteFile, scss,
                   share(@"--bit-clr-#\{\$role\}-fg: color-mix\(in srgb, var\(--bit-clr-#\{\$role\}\) (\d+)%, var\(--bit-clr-fg-pri\)\);", "foreground"),
                   share(@"--bit-clr-#\{\$role\}-tint: color-mix\(in srgb, var\(--bit-clr-#\{\$role\}\) (\d+)%, transparent\);", "tint"));
    }

    // color-mix(in srgb, a share%, b): each gamma-encoded channel interpolated, as the browser does it.
    private static string Mix(string a, string b, double share)
    {
        int channel(string hex, int index) => int.Parse(hex.AsSpan(1 + index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return "#" + string.Concat(Enumerable.Range(0, 3).Select(i => ((int)Math.Round(channel(a, i) * share + channel(b, i) * (1 - share))).ToString("X2", CultureInfo.InvariantCulture)));
    }
}
