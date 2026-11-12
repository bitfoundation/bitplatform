using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ButtonGroup;

/// <summary>
/// Pins the public --bit-ButtonGroup-* variables against the stylesheet that reads them, which a bUnit render cannot
/// see: every variable the header documents is read with a fallback and never declared (so it keeps inheriting from
/// :root and the ancestors), and each one ranks below the parameter written on the group itself.
/// </summary>
[TestClass]
public partial class BitButtonGroupStylesheetTests
{
    [TestMethod]
    public void BitButtonGroupShouldReadEveryPublicVariableItDocumentsWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");
        Assert.AreSequenceEqual(documented, read, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The variables the header documents are not the ones the stylesheet reads.");

        Assert.IsFalse(DeclaredVariable().IsMatch(SourceFiles.StripScssComments(stylesheet)), "A public --bit-ButtonGroup-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitButtonGroupShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these on the group, so the buttons read them before the variable, which only
        // restyles the medium group an unset one stands for.
        Assert.Contains("padding: var(--bit-btg-itm-padding, var(--bit-ButtonGroup-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-md}));", stylesheet);
        Assert.Contains("min-height: var(--bit-btg-itm-min-height, var(--bit-ButtonGroup-min-height, #{$siz-ctrl-md}));", stylesheet);
        Assert.Contains("--bit-btg-itm-fs: var(--bit-btg-itm-fontsize, var(--bit-ButtonGroup-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("--bit-btg-itm-ics: var(--bit-btg-itm-iconsize, var(--bit-ButtonGroup-icon-size, #{$siz-icon-md}));", stylesheet);
        Assert.Contains("font-size: var(--bit-btg-itm-bdg-fs, var(--bit-ButtonGroup-badge-font-size, #{$tg-fs-xs}));", stylesheet);
        Assert.Contains("padding: var(--bit-btg-itm-pad-y, var(--bit-ButtonGroup-padding, #{$siz-ctrl-pad-y-md}));", stylesheet);

        // Rounded is a choice as well, so its full radius outranks the public radius, on the group and on the detached
        // buttons alike.
        Assert.Contains("border-radius: var(--bit-btg-rad, var(--bit-ButtonGroup-radius, #{$shp-radius-button}));", stylesheet);
        Assert.Contains("--bit-btg-rad: #{$shp-radius-full};", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-rnd {"));

        // So does an explicit Color, for every color it paints - read on the button itself, where a variable set on the
        // Style of a single button arrives, with the default of the variant after it. The hover and press states set the
        // variables the rules at rest paint from (--bit-btg-itm-sfg / -sbg / -sbr), never the colors themselves, so the
        // state's value is read ahead of the rest value it stands in for.
        Assert.Contains("color: var(--bit-btg-itm-sfg, var(--bit-btg-itm-clr-txt, var(--bit-ButtonGroup-fill-color, var(--bit-ButtonGroup-color, var(--bit-btg-dfl-txt)))));", stylesheet);
        Assert.Contains("background-color: var(--bit-btg-itm-sbg, var(--bit-btg-itm-clr-bg, var(--bit-ButtonGroup-fill-background, var(--bit-ButtonGroup-background, var(--bit-btg-dfl-bg)))));", stylesheet);
        Assert.Contains("border-color: var(--bit-btg-clr-outer, var(--bit-ButtonGroup-fill-border-color, var(--bit-ButtonGroup-border-color, var(--bit-btg-dfl-outer))));", stylesheet);
        Assert.Contains("--bit-btg-sep-lin: var(--bit-btg-itm-clr-brd, var(--bit-ButtonGroup-fill-separator-color, var(--bit-ButtonGroup-separator-color, var(--bit-ButtonGroup-fill-border-color, var(--bit-ButtonGroup-border-color, var(--bit-btg-dfl-brd))))));", stylesheet);
        Assert.Contains("--bit-btg-itm-sbg: var(--bit-btg-itm-clr-bg-hover, var(--bit-ButtonGroup-fill-hover-background, var(--bit-ButtonGroup-hover-background, #{$clr-pri-hover})));", stylesheet);
        Assert.Contains("background-color: var(--bit-btg-itm-sbg, var(--bit-btg-clr-dark, var(--bit-ButtonGroup-selected-background, #{$clr-pri-dark})));", stylesheet);

        // The variables the states move start out unset on each button, so one nested in another never inherits its state.
        var item = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-itm {");
        Assert.Contains("--bit-btg-itm-sfg: initial;", item);
        Assert.Contains("--bit-btg-itm-sbg: initial;", item);
        Assert.Contains("--bit-btg-itm-sbr: initial;", item);
        Assert.Contains("background-color: var(--bit-btg-itm-clr-bg-dis, var(--bit-ButtonGroup-fill-disabled-background, var(--bit-ButtonGroup-disabled-background, var(--bit-btg-dfl-bg-dis))));", stylesheet);
        Assert.Contains("focus-ring-own(var(--bit-btg-clr-focus, var(--bit-ButtonGroup-focus-color)))", stylesheet);

        // The default of each variant is the primary role, and what a Color does not paint - the transparent background
        // of Outline - has no value of the Color's in front of the variable.
        var fill = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-fil {");
        Assert.Contains("--bit-btg-itm-clr-bg: var(--bit-btg-clr);", fill);
        Assert.Contains("--bit-btg-dfl-bg: #{$clr-pri};", fill);
        var outline = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btg-otl {");
        Assert.Contains("--bit-btg-itm-clr-bg: initial;", outline);
        Assert.Contains("--bit-btg-dfl-bg: transparent;", outline);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-ButtonGroup-[a-z-]+, var\(--bit-btg-(?!dfl-)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", "bit-btg-fil", "bit-btg-ifl"),
        DataRow("outline", "bit-btg-otl", "bit-btg-iol"),
        DataRow("text", "bit-btg-txt", "bit-btg-itx")]
    public void BitButtonGroupShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string variantClass, string itemVariantClass)
    {
        var stylesheet = ReadStylesheet();

        // The group's outer border and the colors its separators pick between are read on the group, by the variant's
        // own rule; the colors of the buttons on each button, by the rule of the variant class every button carries -
        // one class, so it weighs no more than .bit-btg-itm, it finds a button whatever element wraps it, and a variable
        // set on the Style of a single button still arrives.
        var group = SourceFiles.GetScssBlock(stylesheet, $"\n.{variantClass} {{");
        var buttons = SourceFiles.GetScssBlock(stylesheet, $"\n.{itemVariantClass} {{");

        foreach (var name in new[] { "border-color", "separator-color", "disabled-border-color" })
        {
            Assert.Contains($"var(--bit-ButtonGroup-{variant}-{name}, var(--bit-ButtonGroup-{name}, ", group, $".{variantClass} does not read --bit-ButtonGroup-{variant}-{name} ahead of --bit-ButtonGroup-{name}.");
        }

        foreach (var name in new[] { "color", "background", "hover-color", "hover-background", "active-background",
                                     "disabled-background", "disabled-border-color" })
        {
            Assert.Contains($"var(--bit-ButtonGroup-{variant}-{name}, var(--bit-ButtonGroup-{name}, ", buttons, $"The buttons of .{variantClass} do not read --bit-ButtonGroup-{variant}-{name} ahead of --bit-ButtonGroup-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        foreach (var block in new[] { group, buttons })
        {
            Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-ButtonGroup-{variant}-[a-z-]+, )var\(--bit-ButtonGroup-(?:hover-|active-|disabled-|separator-)?(?:color|background|border-color)\b"),
                           $".{variantClass} reads a shared color variable without its own tier ahead of it.");
            Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-ButtonGroup-(?!{variant}-)(?:fill|outline|text)-"),
                           $".{variantClass} reads the variables of another variant.");
        }

        // No rule shared by the three variants reads a color they each have a tier of: the group and the buttons read
        // only what their variant resolved.
        var shared = SourceFiles.StripScssComments(SourceFiles.GetScssBlock(stylesheet, "\n.bit-btg {") + SourceFiles.GetScssBlock(stylesheet, "\n.bit-btg-itm {"));
        Assert.IsFalse(Regex.IsMatch(shared, @"var\(--bit-ButtonGroup-(?:hover-|active-|separator-)?(?:color|background|border-color)\b|var\(--bit-ButtonGroup-disabled-(?:background|border-color)\b"),
                       "A rule shared by the variants reads a color variable that has a tier per variant.");
    }

    [TestMethod]
    public void BitButtonGroupShouldPublishItsColorSizeAndRoundedOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-btg {");

        // A group can sit in the template of another one's button, which must not inherit the outer group's Color, Size
        // or Rounded: each root starts the values those classes publish out unset, and the classes - declared further
        // down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-btg-clr", "--bit-btg-clr-fg", "--bit-btg-clr-txt", "--bit-btg-clr-brd", "--bit-btg-clr-hover", "--bit-btg-clr-active",
                                         "--bit-btg-clr-focus", "--bit-btg-clr-dark", "--bit-btg-clr-dark-hover", "--bit-btg-clr-dark-active",
                                         "--bit-btg-clr-dis", "--bit-btg-clr-dis-text", "--bit-btg-itm-min-height", "--bit-btg-itm-pad-y",
                                         "--bit-btg-itm-padding", "--bit-btg-itm-fontsize", "--bit-btg-itm-bdg-fs", "--bit-btg-itm-iconsize", "--bit-btg-rad" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    [TestMethod,
        DataRow("bit-btg-otl"),
        DataRow("bit-btg-txt")]
    public void BitButtonGroupShouldDrawTheLabelsOfAnUnfilledVariantInTheRoleTextColor(string variantClass)
    {
        var stylesheet = ReadStylesheet();

        // The labels of an Outline or Text group sit on the page rather than on the role's fill, so they are drawn in the
        // role's text color: a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
        var group = SourceFiles.GetScssDeclarations(stylesheet, $"\n.{variantClass} {{");
        Assert.Contains("--bit-btg-itm-clr-txt: var(--bit-btg-clr-fg, var(--bit-btg-clr));", group);
        Assert.Contains("--bit-btg-dfl-txt: #{$clr-pri};", group);

        // Only the labels: the focus ring keeps the role's main.
        Assert.Contains("--bit-btg-itm-clr-fcs: var(--bit-btg-clr);", group);

        Assert.Contains("--bit-btg-clr-fg: #{role-text($role, $tokens)};", SourceFiles.GetScssBlock(stylesheet, "\n    .bit-btg-#{$role} {"));

        // The surface roles are written out by hand, with the text color role-text gives them: the fg of the role map - the
        // on-color a background or border role is paired with (its main is the page, or a hairline on it) and the main of a
        // foreground one.
        foreach (var (role, text) in new[] { ("pbg", "fg-pri"), ("sbg", "fg-pri"), ("tbg", "fg-pri"), ("pfg", "fg-pri"), ("sfg", "fg-sec"),
                                             ("tfg", "fg-ter"), ("pbr", "fg-pri"), ("sbr", "fg-pri"), ("tbr", "fg-pri") })
        {
            Assert.Contains($"--bit-btg-clr-fg: #{{$clr-{text}}};", SourceFiles.GetScssDeclarations(stylesheet, $"\n.bit-btg-{role} {{"));
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "ButtonGroup", "BitButtonGroup.scss");

    [GeneratedRegex(@"^//\s+(--bit-ButtonGroup-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-ButtonGroup-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-ButtonGroup-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
