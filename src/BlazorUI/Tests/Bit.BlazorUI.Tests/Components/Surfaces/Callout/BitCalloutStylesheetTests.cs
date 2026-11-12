using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

/// <summary>
/// Pins the contract of the public --bit-Callout-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit from :root or arrive on the Style of an instance), and the
/// parameters the instance asks for itself win over a value inherited from :root.
/// </summary>
[TestClass]
public class BitCalloutStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Callout-background",
        "--bit-Callout-color",
        "--bit-Callout-border-width",
        "--bit-Callout-border-color",
        "--bit-Callout-radius",
        "--bit-Callout-shadow",
        "--bit-Callout-padding",
        "--bit-Callout-arrow-size",
        "--bit-Callout-divider-color",
        "--bit-Callout-focus-color",
        "--bit-Callout-overlay-background",
    ];

    [TestMethod]
    public void BitCalloutShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.Contains($"var({variable}, ", stylesheet, $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        // Every public variable the stylesheet reads is one the list above (and the demo page) documents.
        var read = Regex.Matches(stylesheet, @"var\((--bit-Callout-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitCalloutShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        // The demo page's CSS variables table is the only source of these names the site and the MCP server have,
        // so a variable the stylesheet reads but the table leaves out is one nobody gets to know about.
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Surfaces", "Callout", "BitCalloutDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-Callout-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(PublicVariables, documented, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public void BitCalloutShouldPaintTheArrowFromTheSameVariablesAsTheCallout()
    {
        var stylesheet = ReadStylesheet();

        // The beak is a sibling of the callout, so both include the one block the surface variables resolve in.
        Assert.Contains("@include clo-surface-vars;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-clo-cal {"));
        Assert.Contains("@include clo-surface-vars;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-clo-arw {"));

        foreach (var selector in new[] { "\n.bit-clo-cal {", "\n.bit-clo-arw {" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, selector);

            Assert.Contains("background-color: var(--bit-clo-bg);", block);
            Assert.Contains("border: var(--bit-clo-brd-w) $shp-border-style var(--bit-clo-brd-c);", block);
        }

        // ArrowSize is written inline as the private variable, which wins over the one a theme sets.
        Assert.Contains("width: var(--bit-clo-arw-siz, var(--bit-Callout-arrow-size, #{spacing(1.5)}));", SourceFiles.GetScssBlock(stylesheet, "\n.bit-clo-arw {"));
    }

    [TestMethod]
    public void BitCalloutShouldLetTheSurfaceParametersWinOverAnInheritedVariable()
    {
        var stylesheet = ReadStylesheet();

        // Background and Border repaint the private variables on the element itself, after the block that resolves
        // them from the public ones, so a value inherited from :root does not undo the parameter.
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-clo-bsg {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-clo-cal {", System.StringComparison.Ordinal));

        Assert.Contains("--bit-clo-bg: #{$clr-bg-sec};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-clo-bsg {"));
        Assert.Contains("--bit-clo-brd-w: var(--bit-Callout-border-width, #{$shp-border-width});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-clo-brd {"));
        Assert.Contains("&.bit-clo-nsh {\n        box-shadow: none;", stylesheet);
    }

    [TestMethod]
    public void BitCalloutShouldStayOutlinedInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        // Forced colors strip the elevation, which is all that tells a borderless callout apart from the page.
        Assert.Contains("@media (forced-colors: active) {\n    .bit-clo-cal,\n    .bit-clo-arw {\n        border-color: CanvasText;", stylesheet);
    }

    private static string ReadStylesheet()
    {
        return SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Callout", "BitCallout.scss");
    }
}
