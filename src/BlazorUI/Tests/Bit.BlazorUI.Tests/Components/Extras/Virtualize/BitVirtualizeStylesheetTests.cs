using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

/// <summary>
/// Pins the public --bit-Virtualize-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting
/// from :root), nothing is read that the header does not document, and the list and its items show where the focus
/// is - in a forced palette too.
/// </summary>
[TestClass]
public partial class BitVirtualizeStylesheetTests
{
    [TestMethod]
    public void BitVirtualizeShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(6, documented.Length, "The stylesheet does not document the six public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitVirtualizeShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitVirtualizeShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Virtualize-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitVirtualizeShouldRingTheListAndItsItemsInsideTheirBox()
    {
        var stylesheet = ReadStylesheet();
        var ring = Block(stylesheet, "\n@mixin vir-ring {");

        // The list clips what overflows it and the items are flush with each other, so the ring is drawn inside, as an
        // outline that is painted above the content of the item.
        StringAssert.Contains(Block(stylesheet, "\n@mixin vir-focus-ring {"), "&:focus-visible {");
        StringAssert.Contains(ring, "outline-offset: calc(-1 * #{$shp-focus-ring-width});");
        Assert.IsFalse(ring.Contains("box-shadow"), "The ring is a box-shadow, which the content of an item covers.");

        // The forced palette turns the ring into the system's own focus color.
        StringAssert.Contains(ring, "outline: #{$shp-focus-ring-width} solid Highlight;");

        StringAssert.Contains(Block(stylesheet, "\n.bit-vir {"), "@include vir-focus-ring;");
        StringAssert.Contains(Block(stylesheet, "\n.bit-vir-itm {"), "@include vir-focus-ring;");
    }

    [TestMethod]
    public void BitVirtualizeStickyItemShouldShowTheRingOfTheFocusedItemItCovers()
    {
        // The pinned copy sits above the real item, so the ring of the item would be hidden under it.
        var copy = Block(ReadStylesheet(), "\n.bit-vir-spc:has(> .bit-vir-blk > .bit-vir-itm[tabindex=\"0\"]:focus-visible) > .bit-vir-sac {");

        StringAssert.Contains(copy, "@include vir-ring;");
    }

    [TestMethod]
    public void BitVirtualizeStickyItemShouldHideTheItemsScrollingUnderIt()
    {
        var sticky = Block(ReadStylesheet(), "\n.bit-vir-stk {");

        StringAssert.Contains(sticky, "background: var(--bit-Virtualize-sticky-background, #{$clr-bg-pri});");
    }

    private static string Block(string stylesheet, string opening)
    {
        var start = stylesheet.IndexOf(opening, System.StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"No rule opens with {opening.Trim()}.");

        var indent = opening[1..].Length - opening[1..].TrimStart().Length;
        var end = stylesheet.IndexOf("\n" + new string(' ', indent) + "}", start + opening.Length, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Virtualize", "BitVirtualize.scss");

    [GeneratedRegex(@"^//\s+(--bit-Virtualize-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Virtualize-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Virtualize-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
