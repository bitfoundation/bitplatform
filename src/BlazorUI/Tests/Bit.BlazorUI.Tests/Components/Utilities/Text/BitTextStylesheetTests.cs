using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Text;

/// <summary>
/// Pins the parts of the text that live in its stylesheet, which a bUnit render cannot see: the public CSS variables
/// (each read with a fallback and never declared, listed in the header of the stylesheet and in the table of the demo
/// page, and ranked below the parameter that does the same job) and the focus behavior of a visually hidden text.
/// </summary>
[TestClass]
public class BitTextStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Text-color",
        "--bit-Text-font-family",
        "--bit-Text-heading-font-family",
        "--bit-Text-monospace-font-family",
        "--bit-Text-code-background",
        "--bit-Text-code-radius",
        "--bit-Text-code-padding",
        "--bit-Text-code-font-size",
        "--bit-Text-gutter",
        "--bit-Text-decoration-color",
        "--bit-Text-decoration-thickness",
        "--bit-Text-underline-offset",
        "--bit-Text-disabled-opacity",
    ];

    [TestMethod]
    public void BitTextShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(stylesheet, @"var\((--bit-Text-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({variable}, [^)]"), $"{variable} is read without a fallback.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitTextShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Utilities", "Text", "BitTextDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Text-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    // The variables restyle the defaults; a parameter is a choice made on the text itself. The color and the family are
    // read on the root, and the classes of Color, Foreground, Gradient, Monospace and the inherit variant all set the
    // same property with the same specificity further down the file, so they win by order alone.
    [TestMethod,
        DataRow("color: var(--bit-Text-color", ".bit-txt-#{$role} {"),
        DataRow("color: var(--bit-Text-color", "\n.bit-txt-rfg {"),
        DataRow("color: var(--bit-Text-color", "\n.bit-txt-grd {"),
        DataRow("font-family: var(--bit-Text-font-family", "\n@each $name, $declarations in $bit-txt-variants {"),
        DataRow("font-family: var(--bit-Text-font-family", "\n.bit-txt-mno {"),
        DataRow("font-family: var(--bit-Text-heading-font-family", "\n.bit-txt-mno {"),
        DataRow("font-family: var(--bit-Text-font-family", "\n.bit-txt-cod {"),
        DataRow("font-family: var(--bit-Text-heading-font-family", "\n.bit-txt-cod {")]
    public void BitTextShouldLetAParameterWinOverItsPublicVariable(string variableRule, string parameterRule)
    {
        var stylesheet = ReadStylesheet();

        var root = stylesheet.IndexOf("\n.bit-txt {", StringComparison.Ordinal);
        var variable = stylesheet.IndexOf(variableRule, StringComparison.Ordinal);
        var parameter = stylesheet.IndexOf(parameterRule, StringComparison.Ordinal);

        Assert.IsTrue(root >= 0 && variable > root, $"\"{variableRule}\" is not read on the root.");
        Assert.IsTrue(parameter > variable, $"\"{parameterRule.Trim()}\" is declared before the root and loses to its variable.");
    }

    // A visually hidden text the keyboard lands on - a skip link, or one inside it - has to be drawn while it holds
    // the focus (WCAG 2.4.7), while one focused by script (tabindex -1, as FocusOnNavigate leaves a page's title)
    // stays hidden. The condition sits in :where, so the rule weighs one class and a page's own class overrides it.
    [TestMethod]
    public void BitTextShouldRevealAVisuallyHiddenTextWhileItHoldsTheKeyboardFocus()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "\n.bit-txt-vhd:where(:not(:focus-visible:not([tabindex=\"-1\"]), :has(:focus-visible:not([tabindex=\"-1\"])))) {");
        Assert.IsFalse(stylesheet.Contains("\n.bit-txt-vhd {", StringComparison.Ordinal), "The visually hidden rules apply to a focused text as well.");
    }

    // A forced-colors mode paints every text in CanvasText, so the dimming alone would be all that says a text is
    // disabled; GrayText is the system color for it, as on a disabled label or link. The rule sits inside the
    // disabled one, which outranks the forced-colors rules of the gradient classes by specificity.
    [TestMethod]
    public void BitTextShouldPaintADisabledTextInGrayTextInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        var disabled = SourceFiles.GetScssBlock(stylesheet, "\n.bit-txt.bit-dis {");

        StringAssert.Contains(disabled, "@media (forced-colors: active) {");
        StringAssert.Contains(disabled, "color: GrayText;");
    }

    // A background role is Canvas under a forced palette, and Chromium draws a Canvas backplate behind the text, so a
    // text colored with one would vanish; it is painted in CanvasText there instead. The roles are the ones the shared
    // role map says are background colors, so a background role added to it is covered as well.
    [TestMethod]
    public void BitTextShouldPaintABackgroundRoleTextInCanvasTextInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Matches(stylesheet, new Regex(@"@media \(forced-colors: active\) \{\s*@each \$role in \$bit-background-color-roles \{\s*\.bit-txt-#\{\$role\} \{\s*color: CanvasText;"));
    }

    // The ring is for a text the keyboard can tab to; a text focused only by script (tabindex -1, as FocusOnNavigate
    // leaves a page's title) keeps whatever outline the app gives the element, so its "h1:focus { outline: none }"
    // still removes it.
    [TestMethod]
    public void BitTextShouldDrawTheSharedFocusRing()
    {
        var stylesheet = ReadStylesheet();

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-txt {");

        StringAssert.Contains(root, "&:focus-visible:not([tabindex=\"-1\"]) {");
        StringAssert.Contains(root, "@include focus-ring;");
        Assert.IsFalse(root.Contains("&:focus-visible {", StringComparison.Ordinal), "A text focused by script is drawn with the ring.");
    }



    // A text is read on the page rather than over the role's fill, so a Color paints it in the role's text color: a
    // secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
    [TestMethod]
    public void BitTextShouldPaintAColorInTheRoleTextColor()
    {
        var roles = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-txt-#{$role} {");

        StringAssert.Contains(roles, "color: role-text($role, $tokens);");
        Assert.IsFalse(roles.Contains("role($tokens, main)", StringComparison.Ordinal), "A Color paints the text in the role's fill color.");
    }

    // The per-breakpoint variants share no weight with anything: one class each, in queries that add none, so the order
    // of the file is what has them win over the plain variants and lose to the classes of every parameter that refines
    // a look - a Weight, a Transform or a Monospace has to win at every width, as Gutter, Block, LineClamp and
    // VisuallyHidden do over the margin and the display.
    [TestMethod,
        DataRow("\n.bit-txt-fwl {"),
        DataRow("\n.bit-txt-trn {"),
        DataRow("\n.bit-txt-mno {"),
        DataRow("\n.bit-txt-gutter {"),
        DataRow("\n.bit-txt-blk {"),
        DataRow("\n.bit-txt-clp {"),
        DataRow("\n.bit-txt-vhd:where(")]
    public void BitTextShouldDeclareTheBreakpointVariantsBetweenThePlainVariantsAndTheRefinements(string refinement)
    {
        var stylesheet = ReadStylesheet();

        var plain = stylesheet.IndexOf("\n@each $name, $declarations in $bit-txt-variants {", StringComparison.Ordinal);
        var breakpoints = stylesheet.IndexOf("\n@each $bp, $min-width in $bit-txt-breakpoints {", StringComparison.Ordinal);
        var refining = stylesheet.IndexOf(refinement, StringComparison.Ordinal);

        Assert.IsTrue(plain >= 0 && breakpoints > plain, "The breakpoint variants are declared before the plain ones and lose to them.");
        Assert.IsTrue(refining > breakpoints, $"\"{refinement.Trim()}\" is declared before the breakpoint variants and loses to them.");
    }

    // Read off the compiled bundle, since the rules are generated: every variant at every breakpoint above the extra
    // small one, inside a min-width query of the width BitGrid and BitStack use, the queries in ascending order so a
    // wider breakpoint wins over a narrower one. The extra small variant is drawn through the plain variant class.
    [TestMethod]
    public void BitTextShouldDrawEveryVariantFromEveryBreakpointUp()
    {
        var css = ReadBundle();
        var previous = -1;

        foreach (var (breakpoint, width) in Breakpoints)
        {
            var start = css.IndexOf($"@media(min-width: {width}px){{.bit-txt-{breakpoint}-", StringComparison.Ordinal);

            Assert.IsTrue(start > previous, $"The {breakpoint} query is missing or comes before a narrower one.");
            previous = start;

            var block = SourceFiles.GetScssBlock(css, $"@media(min-width: {width}px){{.bit-txt-{breakpoint}-");

            foreach (var name in Enum.GetNames<BitTypography>().Select(n => n.ToLowerInvariant()))
            {
                StringAssert.Contains(block, $".bit-txt-{breakpoint}-{name}{{margin:var(--bit-tpg-{name}-margin);", $"{name} has no rule at {breakpoint}.");

                var rule = block[block.IndexOf($".bit-txt-{breakpoint}-{name}{{margin:", StringComparison.Ordinal)..];
                rule = rule[..rule.IndexOf('}')];

                foreach (var property in new[] { "font-size", "font-weight", "line-height", "letter-spacing" })
                {
                    StringAssert.Contains(rule, $"{property}:var(--bit-tpg-{name}-{property})", $"{name} at {breakpoint} does not read its {property}.");
                }
            }

            // A heading is drawn in the heading family and every other variant but the inherit one in the text family,
            // whatever family the variant below it was drawn in.
            StringAssert.Contains(block, $".bit-txt-{breakpoint}-h6{{", $"h6 has no rule at {breakpoint}.");
            StringAssert.Matches(block, new Regex($@"\.bit-txt-{breakpoint}-h1,[^{{]*\.bit-txt-{breakpoint}-h6\{{font-family:var\(--bit-Text-heading-font-family,"));
            StringAssert.Matches(block, new Regex($@"\.bit-txt-{breakpoint}-subtitle1,[^{{]*\.bit-txt-{breakpoint}-overline\{{font-family:var\(--bit-Text-font-family,"));
        }

        Assert.IsFalse(css.Contains("bit-txt-xs-", StringComparison.Ordinal), "The extra small variant has rules of its own.");
    }

    // The display and the case are the two declarations only some variants make (the button, the overline, and the
    // case of the inherit one). A variant without one takes it back to the element's own only where a narrower tier of
    // the same text declared it, so a value the page gives the element is not taken away from every responsive text.
    [TestMethod]
    public void BitTextShouldTakeBackADisplayOrACaseOnlyWhereANarrowerVariantDeclaredIt()
    {
        var css = ReadBundle();
        var narrower = new Dictionary<string, List<string>>
        {
            ["display"] = [".bit-txt-button", ".bit-txt-overline"],
            ["text-transform"] = [".bit-txt-button", ".bit-txt-overline", ".bit-txt-inherit"],
        };

        foreach (var (breakpoint, width) in Breakpoints)
        {
            var block = SourceFiles.GetScssBlock(css, $"@media(min-width: {width}px){{.bit-txt-{breakpoint}-");

            foreach (var (property, classes) in narrower)
            {
                var reset = Regex.Match(block, $@":is\(([^)]*)\):where\(([^)]*)\)\{{{property}:revert\}}");

                Assert.IsTrue(reset.Success, $"No {property} reset at {breakpoint}.");

                var targets = reset.Groups[1].Value.Split(',');
                CollectionAssert.Contains(targets, $".bit-txt-{breakpoint}-h1");
                CollectionAssert.DoesNotContain(targets, $".bit-txt-{breakpoint}-button");
                CollectionAssert.DoesNotContain(targets, $".bit-txt-{breakpoint}-overline");
                CollectionAssert.AreEqual(classes, reset.Groups[2].Value.Split(','), $"The {property} reset at {breakpoint} does not name the narrower tiers.");

                classes.AddRange(classes.Where(c => c.Count(ch => ch == '-') == 2).Select(c => c.Replace("bit-txt-", $"bit-txt-{breakpoint}-")).ToList());
            }
        }
    }

    private static readonly (string Name, int Width)[] Breakpoints = [("sm", 600), ("md", 960), ("lg", 1280), ("xl", 1920), ("xxl", 2560)];

    private static string ReadBundle() => SourceFiles.Read("Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Text", "BitText.scss");
}
