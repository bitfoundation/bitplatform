using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Header;

/// <summary>
/// Pins what a bUnit render cannot see of the header: that a parameter written on it ranks above the public
/// --bit-Header-* variable restyling the same thing, and that a header nested in another one does not inherit its choice.
/// </summary>
[TestClass]
public class BitHeaderStylesheetTests
{
    [TestMethod]
    public void BitHeaderShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color or Size publishes these only while it is set, so they are read before the variable, which
        // only restyles the default an unset one stands for: the plain page surface and the medium spacing.
        // The colors are read per variant, the variant's own tier of each public variable ahead of the shared one, with
        // what a scrolled header publishes (--bit-hdr-scr-*) between the Color and the two tiers.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-hdr-bg, var(--bit-hdr-scr-bg, var(--bit-Header-fill-background, var(--bit-Header-background, #{$clr-bg-pri}))));");
        StringAssert.Contains(stylesheet, "color: var(--bit-hdr-on, var(--bit-Header-fill-color, var(--bit-Header-color, #{$clr-fg-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-hdr-fg, var(--bit-Header-outline-color, var(--bit-Header-color, #{$clr-fg-pri})));");
        StringAssert.Contains(stylesheet, "var(--bit-hdr-brd, var(--bit-hdr-scr-brd, var(--bit-Header-outline-border-color, var(--bit-Header-border-color, #{$clr-brd-pri}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-hdr-bg-dis, var(--bit-Header-fill-disabled-background, var(--bit-Header-disabled-background, #{$clr-bg-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-hdr-fg-dis, var(--bit-Header-text-disabled-color, var(--bit-Header-disabled-color, #{$clr-fg-dis})));");
        StringAssert.Contains(stylesheet, "$hdr-pad-y: var(--bit-hdr-pad-y, var(--bit-Header-padding-block, #{spacing(1)}));");
        StringAssert.Contains(stylesheet, "$hdr-pad-x: var(--bit-hdr-pad-x, var(--bit-Header-padding-inline, #{spacing(3)}));");

        // The size classes publish their paddings bare, so they win over the variables.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-sm {"), "--bit-hdr-pad-y: #{spacing(0.5)};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-lg {"), "--bit-hdr-pad-x: #{spacing(4)};");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Header-[a-z-]+, var\(--bit-hdr-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-hdr-fil", new[] { "color", "background", "border-color", "border-width", "disabled-color", "disabled-background" }),
        DataRow("outline", ".bit-hdr-otl", new[] { "color", "border-color", "border-width", "disabled-color" }),
        DataRow("text", ".bit-hdr-txt", new[] { "color", "border-color", "border-width", "disabled-color" })]
    public void BitHeaderShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string[] names)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // The divider of a Bordered header is the variant's too (Outline draws a whole border instead), in a rule whose
        // variant costs no specificity, so a single class an app hands the header still restyles it.
        if (variant != "outline")
        {
            block += SourceFiles.GetScssBlock(stylesheet, $"\n:where({selector}).bit-hdr-brd {{");
        }

        // Each variant reads a tier of its own ahead of every shared variable it draws with, so a theme can set one look
        // apart from the other two on :root - a neutral hairline around the outlined headers without a divider under
        // every filled one.
        foreach (var name in names)
        {
            StringAssert.Contains(block, $"var(--bit-Header-{variant}-{name}, var(--bit-Header-{name}, ",
                                  $"{selector} does not read --bit-Header-{variant}-{name} ahead of --bit-Header-{name}.");
        }

        // A variant reads a shared variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Header-{variant}-[a-z-]+, )var\(--bit-Header-(?:disabled-)?(?:color|background|border-color|border-width)\b"),
                       $"{selector} reads a shared variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Header-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The divider of a Bordered header is drawn by its variant, never by a rule every variant shares.
        Assert.IsFalse(stylesheet.Contains("\n.bit-hdr-brd {"), "A rule of every variant draws the divider.");
    }

    [TestMethod]
    public void BitHeaderShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-hdr {");

        // A header can sit in the content of another one (a toolbar inside a card inside the page header), which must
        // not inherit the outer header's Color or Size: each root starts the values those classes publish out unset,
        // and the classes - declared further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-hdr-bg", "--bit-hdr-on", "--bit-hdr-fg", "--bit-hdr-brd", "--bit-hdr-bg-dis", "--bit-hdr-fg-dis", "--bit-hdr-pad-y", "--bit-hdr-pad-x" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitHeaderShouldRestyleItsScrolledStateThroughPrivateSlots()
    {
        var stylesheet = ReadStylesheet();

        // The scrolled state moves a variable, never the paint: its class publishes the public variables into private
        // slots, which a nested header resets on its own root so it never inherits the outer one's scrolled state.
        var scrolled = SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-scr {");

        StringAssert.Contains(scrolled, "--bit-hdr-scr-bg: var(--bit-Header-scrolled-background);");
        StringAssert.Contains(scrolled, "--bit-hdr-scr-brd: var(--bit-Header-scrolled-border-color);");
        StringAssert.Contains(scrolled, "--bit-hdr-scr-bdf: var(--bit-Header-scrolled-backdrop-filter);");
        Assert.IsFalse(Regex.IsMatch(scrolled, @"^\s*(?:background|border|color|backdrop-filter)", RegexOptions.Multiline), "The scrolled state paints the header itself.");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr {");

        foreach (var property in new[] { "--bit-hdr-scr-bg", "--bit-hdr-scr-brd", "--bit-hdr-scr-bdf" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        // Every variant reads the slots right behind the Color: the Outline and Text ones fill only while scrolled, and
        // the dividers and the outline take the scrolled border color.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-otl {"), "background-color: var(--bit-hdr-scr-bg, transparent);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-txt {"), "background-color: var(--bit-hdr-scr-bg, transparent);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n:where(.bit-hdr-fil).bit-hdr-brd {"), "var(--bit-hdr-brd, var(--bit-hdr-scr-brd, var(--bit-Header-fill-border-color, ");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n:where(.bit-hdr-txt).bit-hdr-brd {"), "var(--bit-hdr-brd, var(--bit-hdr-scr-brd, var(--bit-Header-text-border-color, ");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-hdr-trs.bit-hdr-fil {"), "translucent(var(--bit-hdr-bg, var(--bit-hdr-scr-bg, var(--bit-Header-fill-background, ");

        // The backdrop filter is read on the headers tracking their scrolled state alone, and a Translucent one keeps
        // its own blur, its rule of two classes outranking this one.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-hdr-esc {"), "backdrop-filter: var(--bit-hdr-scr-bdf, none);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-hdr-trs.bit-hdr-fil {"), "backdrop-filter: blur(");
    }

    [TestMethod]
    public void BitHeaderShouldDrawTheTextOfItsBorderlessVariantsInTheRoleTextColor()
    {
        var roles = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n    .bit-hdr-#{$role} {");

        // The text of an Outline or Text header is read on the page rather than over the role's fill, so it takes the
        // role's text color - a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white -
        // while the border and the fill keep the main.
        StringAssert.Contains(roles, "--bit-hdr-fg: #{role-text($role, $tokens)};");
        StringAssert.Contains(roles, "--bit-hdr-brd: #{role($tokens, main)};");
        StringAssert.Contains(roles, "--bit-hdr-bg: #{role($tokens, main)};");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Header", "BitHeader.scss");
}
