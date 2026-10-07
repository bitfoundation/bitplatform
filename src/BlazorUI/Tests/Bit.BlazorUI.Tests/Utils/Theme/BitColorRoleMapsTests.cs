using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the fg slot of the shared $bit-color-roles map, which every component reads for the text of a variant that
/// is not filled (an outlined or text-only message, card or snack bar, a label, a link): that text sits on the
/// page's own surface, so the slot must be a color meant to be read there, whatever the role.
/// </summary>
[TestClass]
public class BitColorRoleMapsTests
{
    [TestMethod,
        DataRow("pri"),
        DataRow("sec"),
        DataRow("ter"),
        DataRow("inf"),
        DataRow("suc"),
        DataRow("wrn"),
        DataRow("swr"),
        DataRow("err")]
    public void BitColorRoleMapsSemanticRolesShouldReadTheirOwnForegroundToken(string role)
    {
        var tokens = GetRole(role);

        // A semantic main is picked to be a fill - a warning amber is under 2:1 on white - so the theme declares the
        // shade of it meant to be read on the page.
        StringAssert.Contains(tokens, "kind: semantic,");
        StringAssert.Contains(tokens, $"fg: $clr-{role}-fg,");
    }

    [TestMethod,
        DataRow("pfg", "$clr-fg-pri"),
        DataRow("sfg", "$clr-fg-sec"),
        DataRow("tfg", "$clr-fg-ter")]
    public void BitColorRoleMapsForegroundRolesShouldReadTheirMainAsText(string role, string main)
    {
        var tokens = GetRole(role);

        // A foreground role is a text color already.
        StringAssert.Contains(tokens, $"main: {main},");
        StringAssert.Contains(tokens, $"fg: {main},");
    }

    [TestMethod,
        DataRow("pbg", "$clr-bg-pri"),
        DataRow("sbg", "$clr-bg-sec"),
        DataRow("tbg", "$clr-bg-ter"),
        DataRow("pbr", "$clr-brd-pri"),
        DataRow("sbr", "$clr-brd-sec"),
        DataRow("tbr", "$clr-brd-ter")]
    public void BitColorRoleMapsBackgroundAndBorderRolesShouldReadTheirOnColorAsText(string role, string main)
    {
        var tokens = GetRole(role);

        // Read as text on the page, a background color is the page's own color and vanishes, and a border color is a
        // hairline's, far under the 4.5:1 of WCAG 1.4.3; the role's on color is the text it is already paired with.
        StringAssert.Contains(tokens, $"main: {main},");
        StringAssert.Contains(tokens, "on: $clr-fg-pri,");
        StringAssert.Contains(tokens, "fg: $clr-fg-pri,");
        Assert.IsFalse(tokens.Contains($"fg: {main},"), $"The {role} role is read as text in its own {main}.");
    }

    /// <summary>
    /// The slots of one role of the map, from its key through the parenthesis that closes it.
    /// </summary>
    private static string GetRole(string role)
    {
        var match = Regex.Match(ReadStylesheet(), $@"\n    {role}: \(\n(.*?)\n    \),", RegexOptions.Singleline);

        Assert.IsTrue(match.Success, $"The {role} role is missing from $bit-color-roles.");

        return match.Groups[1].Value;
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Styles", "color-role-maps.scss");
}
