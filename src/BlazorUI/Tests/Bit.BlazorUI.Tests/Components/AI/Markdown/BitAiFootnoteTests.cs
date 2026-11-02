using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 24: footnotes.</summary>
[TestClass]
public class BitAiFootnoteTests
{
    [TestMethod]
    public void FootnotesAreNumberedInTheOrderTheyAreFirstCited()
    {
        var document = BitAiMarkdownSpec.Parse("Text[^b] and more[^a].\n\n[^a]: Note A.\n[^b]: Note B.");

        Assert.ContainsSingle(document.Blocks);
        Assert.AreEqual(2, document.Footnotes.Count);
        Assert.AreEqual("b", document.Footnotes[0].Label);
        Assert.AreEqual(1, document.Footnotes[0].Number);
        Assert.AreEqual("Note B.", BitAiMarkdownSpec.Text(document.Footnotes[0].Blocks[0]));
        Assert.AreEqual("a", document.Footnotes[1].Label);
        Assert.AreEqual(2, document.Footnotes[1].Number);

        var references = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]).Inlines.OfType<BitAiFootnoteReferenceInline>().ToList();
        CollectionAssert.AreEqual(new[] { 1, 2 }, references.Select(r => r.Number).ToArray());
    }

    [TestMethod]
    public void ACitedFootnoteCountsItsReferences()
    {
        var document = BitAiMarkdownSpec.Parse("a[^1] b[^1]\n\n[^1]: n");
        var footnote = Assert.ContainsSingle(document.Footnotes);
        var references = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]).Inlines.OfType<BitAiFootnoteReferenceInline>().ToList();

        Assert.AreEqual(2, footnote.ReferenceCount);
        CollectionAssert.AreEqual(new[] { 1, 2 }, references.Select(r => r.Ordinal).ToArray());
    }

    [TestMethod]
    public void AnUncitedDefinitionIsDroppedAndAnUndefinedReferenceIsText()
    {
        var document = BitAiMarkdownSpec.Parse("text[^x]\n\n[^y]: gone");

        Assert.IsEmpty(document.Footnotes);
        Assert.AreEqual("text[^x]", BitAiMarkdownSpec.Text(Assert.ContainsSingle(document.Blocks)));
        Assert.IsEmpty(Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]).Inlines.OfType<BitAiFootnoteReferenceInline>());
    }

    [TestMethod]
    public void AFootnoteBodyMaySpanIndentedLinesAndParagraphs()
    {
        var document = BitAiMarkdownSpec.Parse("x[^1]\n\n[^1]: line one\n    line two\n\n    para two\n\nafter");

        CollectionAssert.AreEqual(new[] { "x[^1]", "after" }, document.Blocks.Select(BitAiMarkdownSpec.Text).ToArray());
        var footnote = Assert.ContainsSingle(document.Footnotes);
        CollectionAssert.AreEqual(new[] { "line one\nline two", "para two" }, footnote.Blocks.Select(BitAiMarkdownSpec.Text).ToArray());
    }

    [TestMethod]
    public void AFootnoteMayCiteAnotherFootnote()
    {
        var document = BitAiMarkdownSpec.Parse("x[^1]\n\n[^1]: see[^2]\n[^2]: end");

        Assert.AreEqual(2, document.Footnotes.Count);
        Assert.AreEqual("2", document.Footnotes[1].Label);
    }

    [TestMethod]
    public void FootnotesCanBeTurnedOff()
    {
        var document = BitAiMarkdownSpec.Parse("x[^1]\n\n[^1]: n", new BitAiMarkdownOptions { Footnotes = false });

        Assert.IsEmpty(document.Footnotes);
        Assert.AreEqual(2, document.Blocks.Count);
        Assert.AreEqual("[^1]: n", BitAiMarkdownSpec.Text(document.Blocks[1]));
    }

    [TestMethod]
    public void TheIncrementalParserAgreesAtEveryPrefix() =>
        BitAiMarkdownSpec.AssertIncrementalMatches("Intro[^a].\n\nMore[^b] and again[^a].\n\n[^a]: Note A\n[^b]: Note B\n    continued\n\nend");
}
