using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Layout;

/// <summary>
/// Pins the contract between what the layout writes inline and the stylesheet that reads it, which a bUnit render
/// cannot see: every private --bit-lyt-* value a parameter writes is declared on every root from the public
/// --bit-Layout-* variable of the same thing (so a nested layout never inherits the outer one's parameter, and the
/// inline declaration wins on the root that carries it), the paint of every section is read at zero specificity so a
/// class the app hands a section still wins, and the public variables are read with a fallback and never declared.
/// </summary>
[TestClass]
public class BitLayoutStylesheetTests : BunitTestContext
{
    [TestMethod]
    public void BitLayoutShouldDeclareEveryPrivateValueAParameterWritesFromItsPublicVariable()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-lyt {");

        Assert.Contains("--bit-lyt-hdr-h: var(--bit-Layout-header-height);", root);
        Assert.Contains("--bit-lyt-ftr-h: var(--bit-Layout-footer-height);", root);
        Assert.Contains("--bit-lyt-gap: var(--bit-Layout-gap);", root);
        Assert.Contains("--bit-lyt-pad: var(--bit-Layout-padding);", root);
        Assert.Contains("--bit-lyt-zin: var(--bit-Layout-z-index, #{$zindex-base});", root);
        Assert.Contains("--bit-lyt-brd: var(--bit-Layout-border-width, #{$shp-border-width}) #{$shp-border-style} var(--bit-Layout-border-color, #{$clr-brd-pri});", root);
    }

    [TestMethod]
    public void BitLayoutShouldWriteItsParametersUnderTheNamesTheRootDeclares()
    {
        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.HeaderHeight, 64);
            parameters.Add(p => p.FooterHeight, 40);
            parameters.Add(p => p.Gap, "1rem");
            parameters.Add(p => p.Padding, "2rem");
            parameters.Add(p => p.ZIndex, 7);
        });

        var style = component.Find(".bit-lyt").GetAttribute("style")!;

        // The inline declaration is what makes the parameter win over the public variable: it lands on the same
        // element as the root rule's declaration and outranks it.
        Assert.Contains("--bit-lyt-hdr-h:64px", style);
        Assert.Contains("--bit-lyt-ftr-h:40px", style);
        Assert.Contains("--bit-lyt-gap:1rem", style);
        Assert.Contains("--bit-lyt-pad:2rem", style);
        Assert.Contains("--bit-lyt-zin:7", style);

        // A layout given none of them writes none, so the public variables are what decides.
        var defaulted = RenderComponent<BitLayout>();
        var actualValue = defaulted.Find(".bit-lyt").GetAttribute("style")?.Contains("--bit-lyt-") ?? false;

        Assert.IsFalse(actualValue);
    }

    [TestMethod]
    public void BitLayoutShouldTakeNoRoomForABarItDoesNotRender()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lyt {");

        // A height set once for the whole app says nothing about a page that hides its header, so the offset the
        // pinned panels read is zeroed by the class the component renders when there is no such bar.
        Assert.Contains("--bit-lyt-hdr-o: var(--bit-lyt-hdr-h, 0px);", root);
        Assert.Contains("--bit-lyt-ftr-o: var(--bit-lyt-ftr-h, 0px);", root);
        Assert.Contains("--bit-lyt-hdr-o: 0px;", SourceFiles.GetScssBlock(root, "&.bit-lyt-nhd {"));
        Assert.Contains("--bit-lyt-ftr-o: 0px;", SourceFiles.GetScssBlock(root, "&.bit-lyt-nft {"));

        var component = RenderComponent<BitLayout>(parameters =>
        {
            parameters.Add(p => p.HeaderHeight, 64);
            parameters.Add(p => p.Header, (RenderFragment)(b => b.AddContent(0, "Header")));
        });

        var classes = component.Find(".bit-lyt").ClassList;

        Assert.IsFalse(classes.Contains("bit-lyt-nhd"));
        Assert.IsTrue(classes.Contains("bit-lyt-nft"));
    }

    [TestMethod,
        DataRow(":where(.bit-lyt)", "--bit-Layout-color", "--bit-Layout-background", null),
        DataRow(":where(.bit-lyt-hdr)", "--bit-Layout-header-color", "--bit-Layout-header-background", "--bit-Layout-header-shadow"),
        DataRow(":where(.bit-lyt-ftr)", "--bit-Layout-footer-color", "--bit-Layout-footer-background", "--bit-Layout-footer-shadow"),
        DataRow(":where(.bit-lyt-nmn)", "--bit-Layout-nav-panel-color", "--bit-Layout-nav-panel-background", null),
        DataRow(":where(.bit-lyt-asd)", "--bit-Layout-aside-color", "--bit-Layout-aside-background", null),
        DataRow(":where(.bit-lyt-mcn)", "--bit-Layout-main-color", "--bit-Layout-main-background", null)]
    public void BitLayoutShouldPaintEverySectionAtZeroSpecificity(string selector, string color, string background, string? shadow)
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} {{", $"No rule for {selector}.");

        Assert.Contains($"color: var({color}, inherit);", block);
        Assert.Contains($"background: var({background}, transparent);", block);

        if (shadow is not null)
        {
            Assert.Contains($"box-shadow: var({shadow}, none);", block);
        }
    }

    [TestMethod]
    public void BitLayoutShouldPaintAPinnedBarWithThePageBackgroundUnlessGivenOne()
    {
        var stylesheet = ReadStylesheet();

        // A pinned bar passes over the content scrolling under it, so a transparent one would show the text sliding
        // beneath it; the public variable still wins, and the rule still costs no specificity.
        Assert.Contains("background: var(--bit-Layout-header-background, #{$clr-bg-pri});", SourceFiles.GetScssBlock(stylesheet, "\n:where(.bit-lyt-shd > .bit-lyt-hdr) {"));
        Assert.Contains("background: var(--bit-Layout-footer-background, #{$clr-bg-pri});", SourceFiles.GetScssBlock(stylesheet, "\n:where(.bit-lyt-sft > .bit-lyt-ftr) {"));
    }

    [TestMethod]
    public void BitLayoutShouldReadNoPaintVariableOutsideAZeroSpecificityRule()
    {
        var stylesheet = ReadStylesheet();
        var code = SourceFiles.StripScssComments(stylesheet);

        var paint = new Regex(@"var\(--bit-Layout-[a-z-]*(?:background|color|shadow)\b");

        // Every read of a color, background or shadow variable sits in a :where() rule, so a single class the app
        // hands a section (Class, Classes) outranks it. The border color is the one exception: it is folded into the
        // private --bit-lyt-brd on the root, which the Bordered rules read.
        var inWhere = Regex.Matches(code, @"\n:where\([^\n{]*\) \{")
                           .Select(m => SourceFiles.GetScssBlock(code, m.Value))
                           .Sum(block => paint.Matches(block).Count);

        var everywhere = paint.Matches(code).Count - Regex.Matches(code, @"var\(--bit-Layout-border-color\b").Count;

        Assert.IsGreaterThan(10, everywhere, $"Only {everywhere} paint reads were found.");
        Assert.AreEqual(everywhere, inWhere, "A paint variable is read in a rule that carries specificity.");
    }

    [TestMethod]
    public void BitLayoutShouldNeverDeclareAPublicVariable()
    {
        var code = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(code, @"--bit-Layout-[a-z-]+\s*:"), "A public --bit-Layout-* variable is declared by the stylesheet.");
    }

    [TestMethod]
    public void BitLayoutShouldReadEveryPublicVariableItsHeaderDocuments()
    {
        var stylesheet = ReadStylesheet();
        var code = SourceFiles.StripScssComments(stylesheet);

        var documented = Regex.Matches(stylesheet, @"^//\s+(?<name>--bit-Layout-[a-z-]+)\s", RegexOptions.Multiline)
                              .Select(m => m.Groups["name"].Value)
                              .Distinct()
                              .ToArray();

        // Guards the guard: a header that documents nothing would let every variable through in silence.
        Assert.IsGreaterThanOrEqualTo(20, documented.Length, $"The header documents only {documented.Length} variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}", code, $"{name} is documented but never read.");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Layout", "BitLayout.scss");
}
