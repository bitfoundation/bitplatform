using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

/// <summary>
/// Pins what a bUnit render cannot see of the nav: its public --bit-Nav-* variables, which the stylesheet reads and
/// the header comment of that stylesheet documents, and the few layout rules the rendered markup relies on.
/// </summary>
[TestClass]
public class BitNavStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Nav-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Nav-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Nav-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitNavShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Nav-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitNavShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every nav below
        // it: a declaration on the nav would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitNavShouldLetAGroupHeaderGrowWithItsDescription()
    {
        var header = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-gcb {");

        // A header with a description stacks two lines, which a fixed height would let spill over the rule under it.
        StringAssert.Contains(header, "min-height: var(--bit-Nav-header-min-height");
        Assert.IsFalse(Regex.IsMatch(header, @"(?<![-\w])height\s*:"), "The group header has a fixed height.");
    }

    [TestMethod]
    public void BitNavShouldPutNoRoomBetweenTheChevronAndTheContentOfAnItem()
    {
        // A childless sibling makes up for the chevron with IndentPadding alone, so a gap here would push the text of
        // a parent out of line with it.
        var row = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-mct {");

        Assert.IsFalse(row.Contains("gap"), "The row of an item puts a gap between its chevron and its content.");
    }

    [TestMethod]
    public void BitNavShouldGiveAPressedItemFeedbackOnEveryPointer()
    {
        var item = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-nav-ict {");

        var hover = item.IndexOf("@media (hover: hover)", StringComparison.Ordinal);
        var active = item.IndexOf("&:active {", StringComparison.Ordinal);

        // The hover is kept to the pointers that can hover, the pressed state is not: it is outside that media query.
        Assert.IsTrue(active > 0, "The item has no pressed state.");
        Assert.IsTrue(active > item.IndexOf("\n    }", hover, StringComparison.Ordinal), "The pressed state is kept to the pointers that can hover.");
        StringAssert.Contains(item[active..], "var(--bit-Nav-pressed-background");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "Nav", "BitNav.scss");
}
