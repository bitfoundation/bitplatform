using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

/// <summary>
/// Pins the contract of the public --bit-AccordionList-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit), and the demo page documents every one of them. Also covers
/// what the Joined list asks of the stylesheet.
/// </summary>
[TestClass]
public class BitAccordionListStylesheetTests : BunitTestContext
{
    private static readonly string[] PublicVariables =
    [
        "--bit-AccordionList-gap",
        "--bit-AccordionList-divider-color",
    ];

    [TestMethod]
    public void BitAccordionListShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.Contains($"var({variable}, ", stylesheet, $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        var read = Regex.Matches(stylesheet, @"var\((--bit-AccordionList-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitAccordionListShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "AccordionList", "BitAccordionListDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-AccordionList-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(PublicVariables, documented, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldReachOnlyTheListsOwnItems()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // Every rule of the joined list is chained through the item wrapper, so an accordion nested in a panel keeps
        // its own corners and its own outline. Everything from the joined block on is read, not the block alone, so the
        // checks below that something is absent also cover a joined rule written after it.
        var start = stylesheet.IndexOf("\n.bit-acl-jnd {", System.StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, "The joined list has no rule of its own.");

        var joined = stylesheet[start..];

        foreach (Match rule in Regex.Matches(joined, @"^\s*(>[^{]+)\{", RegexOptions.Multiline))
        {
            Assert.StartsWith("> .bit-acl-itm", rule.Groups[1].Value.Trim(), $"'{rule.Groups[1].Value.Trim()}' does not start at the item wrapper.");
        }

        Assert.Contains("gap: 0;", joined);

        // The shared line is the top border of the lower item alone, so nothing pairs an item with the one above
        // it: no adjacency, and no overlap sized by one item's border for a line that belongs to the other.
        Assert.Contains("border-block-end-width: 0;", joined);
        Assert.DoesNotContain("margin-block-start", joined, "The shared line is drawn by overlapping two borders again.");
        Assert.IsFalse(Regex.IsMatch(joined, @"\+\s*\.bit-acl-itm"), "A rule pairs adjacent items, which markup between the options breaks.");
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldFindTheLastItemWithoutHas()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // :has() is the newest of the selectors that could find it; the last item is found with the older
        // `of S` syntax instead, behind a plain :last-child for the engines that predate that too.
        Assert.DoesNotContain(":has(", stylesheet);
        Assert.Contains("> .bit-acl-itm:last-child > .bit-acd", stylesheet);
        Assert.Contains("> .bit-acl-itm:nth-last-child(1 of .bit-acl-itm) > .bit-acd", stylesheet);
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldSetTheClassAndIgnoreTheGap()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitAccordionListItem> { new() { Title = "A" }, new() { Title = "B" } });
            parameters.Add(p => p.Joined, true);
            parameters.Add(p => p.Gap, 16);
        });

        var root = component.Find(".bit-acl");

        Assert.IsTrue(root.ClassList.Contains("bit-acl-jnd"));
        Assert.DoesNotContain("gap:", (root.GetAttribute("style") ?? string.Empty));

        component.Render(parameters => parameters.Add(p => p.Joined, false));

        root = component.Find(".bit-acl");

        Assert.IsFalse(root.ClassList.Contains("bit-acl-jnd"));
        Assert.Contains("gap:16px", root.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitAccordionListShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());
        var divider = SourceFiles.GetScssBlock(stylesheet, "> .bit-acl-itm ~ .bit-acl-itm > .bit-acd {");

        // The shared line is the top border of the lower item, so an explicit Border - whose class on the item has put
        // its color in --bit-acd-brd - paints it, while the divider variable restyles the line of an unset one.
        Assert.Contains("border-block-start-color: var(--bit-AccordionList-divider-color, var(--bit-acd-brd));", divider);
        var bordered = SourceFiles.GetScssBlock(divider, "&:is(.bit-acd-pbr, .bit-acd-sbr, .bit-acd-tbr, .bit-acd-rbr) {");
        Assert.Contains("border-block-start-color: var(--bit-acd-brd);", bordered);
        Assert.DoesNotContain("--bit-AccordionList-divider-color", bordered, "The divider variable wins over an explicit Border.");

        // The borderless rule comes after it at the same weight, so NoBorder keeps drawing a divider of its own.
        Assert.IsLessThan(divider.IndexOf("&.bit-acd-nbd {", System.StringComparison.Ordinal), divider.IndexOf("&:is(", System.StringComparison.Ordinal));
    }

    private static string ReadStylesheet()
    {
        return SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "AccordionList", "BitAccordionList.scss");
    }
}
