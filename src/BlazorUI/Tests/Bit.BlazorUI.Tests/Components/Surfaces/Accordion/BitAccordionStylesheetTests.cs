using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Accordion;

/// <summary>
/// Pins the contract of the public --bit-Accordion-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit from :root, an ancestor or the Style of an instance), and the
/// parameters the instance asks for itself win over a value inherited from an ancestor.
/// </summary>
[TestClass]
public class BitAccordionStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Accordion-color",
        "--bit-Accordion-background",
        "--bit-Accordion-border-color",
        "--bit-Accordion-border-width",
        "--bit-Accordion-radius",
        "--bit-Accordion-shadow",
        "--bit-Accordion-font-size",
        "--bit-Accordion-header-padding",
        "--bit-Accordion-header-hover-background",
        "--bit-Accordion-header-active-background",
        "--bit-Accordion-header-expanded-background",
        "--bit-Accordion-header-expanded-color",
        "--bit-Accordion-title-color",
        "--bit-Accordion-title-font-size",
        "--bit-Accordion-title-font-weight",
        "--bit-Accordion-description-color",
        "--bit-Accordion-icon-size",
        "--bit-Accordion-icon-color",
        "--bit-Accordion-expander-color",
        "--bit-Accordion-content-padding",
        "--bit-Accordion-content-background",
        "--bit-Accordion-divider-color",
        "--bit-Accordion-focus-color",
        "--bit-Accordion-disabled-color",
        "--bit-Accordion-disabled-background",
    ];

    [TestMethod]
    public void BitAccordionShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = StripComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.IsTrue(stylesheet.Contains($"var({variable}, "), $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        // Every public variable the stylesheet reads is one the list above (and the demo page) documents.
        var read = Regex.Matches(stylesheet, @"var\((--bit-Accordion-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitAccordionShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        // The demo page's CSS variables table is the only source of these names the site and the MCP server have,
        // so a variable the stylesheet reads but the table leaves out is one nobody gets to know about.
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Surfaces", "Accordion", "BitAccordionDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-Accordion-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, documented);
    }

    [TestMethod]
    public void BitAccordionShouldLetBackgroundAndBorderWinOverAnInheritedVariable()
    {
        var stylesheet = ReadStylesheet();

        // The fill and the outline are resolved into private variables on the root, which the Background and Border
        // classes declared after it replace on the same element, so a value inherited from an ancestor does not undo
        // the parameters.
        var root = GetBlock(stylesheet, "\n.bit-acd {");

        StringAssert.Contains(root, "--bit-acd-bg: var(--bit-Accordion-background, #{$clr-bg-pri});");
        StringAssert.Contains(root, "--bit-acd-brd: var(--bit-Accordion-border-color, #{$clr-brd-pri});");
        StringAssert.Contains(root, "background-color: var(--bit-acd-bg);");

        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-tbg {"), "--bit-acd-bg: #{$clr-bg-ter};");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-sbr {"), "--bit-acd-brd: #{$clr-brd-sec};");

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-acd {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-acd-tbg {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitAccordionShouldKeepTheExpandedHeaderColorOffNestedAccordions()
    {
        var stylesheet = ReadStylesheet();

        // Set on the open accordion's own header line, never on the root, where it would inherit into every accordion
        // nested in the panel; and not on a disabled one, which keeps its greyed-out text.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-exp:not(.bit-dis) > .bit-acd-hwr {"), "color: var(--bit-Accordion-header-expanded-color, inherit);");
        StringAssert.DoesNotMatch(GetBlock(stylesheet, "\n.bit-acd {"), new Regex("header-expanded-color"));

        // The hover and pressed shades do not replace an expanded fill, whose text color was picked against it; the
        // rule has the specificity of the generic one and so has to follow it.
        var expandedHover = stylesheet.IndexOf("\n.bit-acd-exp:not(.bit-dis, .bit-acd-rdo) > .bit-acd-hwr {", System.StringComparison.Ordinal);

        Assert.IsTrue(expandedHover > stylesheet.IndexOf("\n.bit-acd:not(.bit-dis, .bit-acd-rdo) > .bit-acd-hwr {", System.StringComparison.Ordinal));
        StringAssert.Contains(stylesheet[expandedHover..], "background-color: var(--bit-Accordion-header-expanded-background, var(--bit-acd-hov));");
        StringAssert.Contains(stylesheet[expandedHover..], "background-color: var(--bit-Accordion-header-expanded-background, var(--bit-acd-prs));");
    }

    [TestMethod]
    public void BitAccordionShouldDrawTheDividerWithoutTakingRoom()
    {
        var stylesheet = ReadStylesheet();

        // An inset shadow on the panel, not a border that would add to the height of a collapsed one; the fill sits
        // on the grid row, which is no taller than what is showing of the panel.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-con {"), "box-shadow: inset 0 var(--bit-Accordion-border-width, #{$shp-border-width}) 0 0 var(--bit-Accordion-divider-color, transparent);");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-cnt {\n    display: grid;"), "background-color: var(--bit-Accordion-content-background, transparent);");
    }

    [TestMethod]
    public void BitAccordionShouldOpenARevealedPanelFromTheStylesheet()
    {
        var stylesheet = ReadStylesheet();

        // A HiddenUntilFound panel is hidden by its attribute alone, and one the browser has revealed (the class
        // without the attribute) is opened by the stylesheet before the render that expands the accordion.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-acd-huf {"), "visibility: inherit;");
        StringAssert.Contains(stylesheet, ".bit-acd-cnt:has(> .bit-acd-cwr > .bit-acd-huf:not([hidden])) {\n    grid-template-rows: 1fr;");
    }

    [TestMethod]
    public void BitAccordionShouldPrintAHiddenUntilFoundPanelWithExpandOnPrint()
    {
        // hidden="until-found" hides the content through content-visibility, which paper would honor too.
        var print = ReadStylesheet()[ReadStylesheet().IndexOf("@media print {", System.StringComparison.Ordinal)..];

        StringAssert.Contains(print, "content-visibility: visible;");
    }

    [TestMethod]
    public void BitAccordionShouldMarkADisabledAccordionInForcedColors()
    {
        StringAssert.Contains(ReadStylesheet(), "@media (forced-colors: active) {\n    .bit-acd.bit-dis > .bit-acd-hwr {");
    }

    private static string StripComments(string stylesheet)
    {
        return Regex.Replace(stylesheet, @"//[^\n]*", string.Empty);
    }

    private static string GetBlock(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet()
    {
        return ReadFile("Bit.BlazorUI", "Components", "Surfaces", "Accordion", "BitAccordion.scss");
    }

    // The path is relative to the BlazorUI folder; the test project copies each file it reads to the same path under
    // the output directory.
    private static string ReadFile(params string[] segments)
    {
        var path = Path.Combine([System.AppContext.BaseDirectory, .. segments]);

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
